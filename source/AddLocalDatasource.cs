using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.SecurityModel;
using Sitecore.Sites;

namespace TheReference.DotNet.Sitecore.LocalDatasources
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Datasource", Justification = "Same spelling as used by sitecore")]
    public class AddLocalDatasource
    {
        private const string RelativePath = "./";

        internal static readonly Guid LocalDataFolderTemplateId = new Guid("{A37C4ADC-A626-4807-ACDD-748AD26C4144}");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This is part of a sitecore pipeline and should not be static")]
        public void Process(GetRenderingDatasourceArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var locations = args.RenderingItem["Datasource Location"].Split('|');
            if (!locations.Any(x => x.Contains(RelativePath)))
            {
                return;
            }

            args.DatasourceRoots.Clear();

            foreach (var datasourceLocation in locations)
            {
                if (string.IsNullOrEmpty(datasourceLocation))
                {
                    continue;
                }

                if (datasourceLocation.StartsWith(RelativePath, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(args.ContextItemPath))
                {
                    CreateDatasource(args, datasourceLocation);
                }
                else
                {
                    args.DatasourceRoots.Add(args.ContentDatabase.GetItem(datasourceLocation));
                }
            }
        }

        private static void CreateDatasource(GetRenderingDatasourceArgs args, string datasourceLocation)
        {
            var itemPath = args.ContextItemPath + datasourceLocation.Remove(0, 1);
            var item = args.ContentDatabase.GetItem(itemPath);
            var contextItem = args.ContentDatabase.GetItem(args.ContextItemPath, args.ContentLanguage);

            if (item == null && contextItem != null)
            {
                item = AddDataFolderItem(datasourceLocation, contextItem);
            }

            args.DatasourceRoots.Add(item);

            var datasourceItem = AddDatasourceItem(args, item);
            if (datasourceItem != null)
            {
                args.CurrentDatasource = datasourceItem.Paths.FullPath;
            }
        }

        private static Item AddDataFolderItem(string datasourceLocation, Item parent)
        {
            var itemName = datasourceLocation.Remove(0, 2);
            var localDataFolderTemplateId = new TemplateID(new ID(LocalDataFolderTemplateId));
            using (new SecurityDisabler())
            {
                using (new SiteContextSwitcher(SiteContextFactory.GetSiteContext("system")))
                {
                    var newItem = parent.Add(itemName, localDataFolderTemplateId);
                    newItem.Editing.BeginEdit();
                    newItem.Appearance.Sortorder = 9999;
                    newItem.Editing.EndEdit(false, false);
                    return newItem;
                }
            }
        }

        private static Item AddDatasourceItem(GetRenderingDatasourceArgs args, Item datasourceFolder)
        {
            var datasourceTemplate = args.RenderingItem["Datasource Template"];
            var datasourceTemplateItem = (TemplateItem) args.ContentDatabase.GetItem(datasourceTemplate);
            var count = datasourceFolder.Children.Count(c => c.TemplateID.Equals(datasourceTemplateItem.ID));

            using (new SecurityDisabler())
            {
                using (new SiteContextSwitcher(SiteContextFactory.GetSiteContext("system")))
                {
                    using (new LanguageSwitcher(args.ContentLanguage))
                    {
                        return datasourceFolder.Add(FormattableString.Invariant($"{datasourceTemplateItem.Name} {count + 1}"), datasourceTemplateItem);
                    }
                }
            }
        }
    }
}
