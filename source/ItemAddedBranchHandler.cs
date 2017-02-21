using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Layouts;
using Sitecore.Xml.Serialization;

namespace TheReference.DotNet.Sitecore.LocalDatasources
{
    public class ItemAddedBranchHandler
    {
        public void OnItemAdded(object sender, EventArgs args)
        {
            var item = Event.ExtractParameter(args, 0) as Item;

            Assert.IsNotNull(item, "item");
            if (item == null)
            {
                return;
            }

            if (item.BranchId == ID.Null)
            {
                return;
            }

            HandleItem(item);

            // include descendants - they will already be created and this function will not be called for them
            var descendants = item.Axes.GetDescendants();
            foreach (var descendant in descendants)
            {
                HandleItem(descendant);
            }
        }

        private static void HandleItem(Item item)
        {
            if (item.Visualization.Layout == null)
            {
                return;
            }

            SetDatasourcesToLocal(item, global::Sitecore.FieldIDs.LayoutField);
        }

        private static void SetDatasourcesToLocal(Item item, ID fieldId)
        {
            var layout = LayoutDefinition.Parse(item[fieldId]);
            var devices = layout.Devices;
            var changed = devices.Cast<DeviceDefinition>().Sum(device => SetDatasourcesToLocal(device, item));

            if (changed > 0)
            {
                UpdateLayout(item, layout, fieldId);
            }
        }

        private static int SetDatasourcesToLocal(DeviceDefinition device, Item item)
        {
            int changed = 0;
            var renderings = device.Renderings;
            foreach (RenderingDefinition rendering in renderings)
            {
                var dataItem = GetRenderingLocalDatasource(rendering, item.Database);
                if (dataItem == null)
                {
                    continue;
                }

                var localSource = GetLocalDatasource(item, dataItem);
                rendering.Datasource = localSource?.ID.Guid.ToString("B").ToUpperInvariant();
                changed++;
            }

            return changed;
        }

        private static Item GetRenderingLocalDatasource(RenderingDefinition rendering, Database database)
        {
            var datasource = rendering.Datasource;
            if (string.IsNullOrEmpty(datasource))
            {
                return null;
            }

            ID id;
            if (!ID.TryParse(datasource, out id))
            {
                return null;
            }

            var dataItem = database.GetItem(id);
            if (dataItem != null && dataItem.Paths.ContentPath.StartsWith("/sitecore/template", StringComparison.OrdinalIgnoreCase))
            {
                return dataItem;
            }

            return null;
        }

        /// <summary>
        /// Gets the datasource underneath the parent item to replace the current source (from the branch)
        /// </summary>
        /// <param name="parent">Parent (content) item</param>
        /// <param name="currentSource">Current datasource</param>
        /// <returns>Local datasource Item</returns>
        /// <remarks>We explicetely go into the local data folder because a search over all descendants might also find a similar datasource on another page in the branch</remarks>
        private static Item GetLocalDatasource(Item parent, Item currentSource)
        {
            var localDatafolder = parent.Children.FirstOrDefault(i => i.TemplateID.Guid.Equals(AddLocalDatasource.LocalDataFolderTemplateId));
            return localDatafolder?.Children.FirstOrDefault(i => i.Name.Equals(currentSource.Name, StringComparison.OrdinalIgnoreCase));
        }

        private static void UpdateLayout(Item item, XmlSerializable layout, ID fieldId)
        {
            item.Editing.BeginEdit();
            item.Fields[fieldId].Value = layout.ToXml();
            item.Editing.EndEdit(false, false);
        }
    }
}
