using System;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.SecurityModel;
using Sitecore.Sites;

namespace TheReference.DotNet.Sitecore.LocalDatasources.Infrastructure.Pipelines
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Datasource", Justification = "Same spelling as used by sitecore")]
    public class AddLocalDatasource
    {
        private const string RelativePath = "./";

        internal static readonly Guid LocalDataFolderTemplateId;

        static AddLocalDatasource()
        {
            string templateIdString = Settings.GetSetting("LocalDatasources.DataFolderTemplateId", "{A37C4ADC-A626-4807-ACDD-748AD26C4144}");
            LocalDataFolderTemplateId = new Guid(templateIdString);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This is part of a sitecore pipeline and should not be static")]
        public void Process(GetRenderingDatasourceArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (!string.IsNullOrEmpty(args.CurrentDatasource))
            {
                return;
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
                    using (new EditContext(newItem, false, false))
                    {
                        newItem.Appearance.Sortorder = 9999;
                    }

                    return newItem;
                }
            }
        }

        private static Item AddDatasourceItem(GetRenderingDatasourceArgs args, Item datasourceFolder)
        {
            string datasourceTemplate = args.RenderingItem["Datasource Template"];

            Item item = args.ContentDatabase.GetItem(datasourceTemplate);
            
            string datasourceName = CreateDatasourceName(datasourceFolder, item);
            
            using (new SecurityDisabler())
            {
                using (new SiteContextSwitcher(SiteContextFactory.GetSiteContext("system")))
                {
                    using (new LanguageSwitcher(args.ContentLanguage))
                    {
                        Item datasourceItem;

                        if (item.TemplateID == TemplateIDs.Template)
                        {
                            datasourceItem = datasourceFolder.Add(datasourceName, (TemplateItem) item);
                        }
                        else if (item.TemplateID == TemplateIDs.BranchTemplate)
                        {
                            datasourceItem = datasourceFolder.Add(datasourceName, (BranchItem) item);
                        }
                        else
                        {
                            throw new ArgumentException($"Datasource Template \"{datasourceTemplate}\" does not correspond to a valid template or branch template.");
                        }

                        Item localizedDatasource = datasourceItem.Database.GetItem(datasourceItem.ID, args.ContentLanguage);

                        if (localizedDatasource.Versions.Count == 0)
                        {
                            using (new EditContext(localizedDatasource))
                            {
                                localizedDatasource.Versions.AddVersion();
                            }
                        }

                        return localizedDatasource;
                    }
                }
            }
        }

        private static string CreateDatasourceName(Item datasourceFolder, Item item)
        {
            var number = 1;
            string datasourceName;

            do
            {
                datasourceName = FormattableString.Invariant($"{item.Name} {number}");
                number++;
            }
            while (datasourceFolder.Children.Any(i => i.Name == datasourceName));

            return datasourceName;
        }
    }
}
