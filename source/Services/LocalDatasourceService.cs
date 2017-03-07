using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Collections;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using TheReference.DotNet.Sitecore.LocalDatasources.Infrastructure.Pipelines;

namespace TheReference.DotNet.Sitecore.LocalDatasources.Services
{
    internal static class LocalDatasourceService
    {
        internal static void UpdateTree(Item source, Item target)
        {
            UpdateItem(source, target);

            var targetChildren = target.Children;
            var sourceChildren = source.Children;
            var count = Math.Min(sourceChildren.Count, targetChildren.Count);

            for (var i = 0; i < count; i++)
            {
                var targetChild = targetChildren[i];
                if (targetChild.Visualization.Layout == null)
                {
                    continue;
                }
                
                var sourceChild = sourceChildren[i];
                if (sourceChild != null)
                {
                    UpdateTree(sourceChild, targetChild);
                }
            }
        }

        private static void UpdateItem(Item source, Item target)
        {
            if (source == null || target?.Visualization.Layout == null)
            {
                return;
            }

            var pairs = GetMatchingLocalSources(source, target).ToList();
            ProcessField(target.Fields[global::Sitecore.FieldIDs.LayoutField], pairs);
            ProcessField(target.Fields[global::Sitecore.FieldIDs.FinalLayoutField], pairs);
        }

        private static IEnumerable<Pair<Item, Item>> GetMatchingLocalSources(Item source, Item target)
        {
            var sourceDataFolder = source.Children.FirstOrDefault(c => c.TemplateID.Guid.Equals(AddLocalDatasource.LocalDataFolderTemplateId));
            if (sourceDataFolder == null)
            {
                yield break;
            }

            var targetDataFolder = target.Children.FirstOrDefault(c => c.TemplateID.Guid.Equals(AddLocalDatasource.LocalDataFolderTemplateId));
            if (targetDataFolder == null)
            {
                yield break;
            }

            var sourceChildren = sourceDataFolder.Children;
            var targetChildren = targetDataFolder.Children;

            if (!sourceChildren.Any() || !targetChildren.Any())
            {
                yield break;
            }

            foreach (Item sourceItem in sourceDataFolder.Children)
            {
                var targetItem = targetChildren.FirstOrDefault(t => t.Name.Equals(sourceItem.Name, StringComparison.OrdinalIgnoreCase));
                if (targetItem != null)
                {
                    yield return new Pair<Item, Item>(sourceItem, targetItem);
                }
            }
        }

        private static void ProcessField(Field field, IEnumerable<Pair<Item, Item>> pairs)
        {
            var initialValue = GetInitialFieldValue(field);
            if (string.IsNullOrEmpty(initialValue))
            {
                return;
            }

            var value = new StringBuilder(initialValue);
            foreach (var itemPair in pairs)
            {
                ReplaceId(itemPair.Part1, itemPair.Part2, value);
                ReplaceShortId(itemPair.Part1, itemPair.Part2, value);
                ReplaceFullPath(itemPair.Part1, itemPair.Part2, value);
                ReplaceContentPath(itemPair.Part1, itemPair.Part2, value);
            }

            UpdateFieldValue(field, initialValue, value.ToString());
        }

        private static string GetInitialFieldValue(Field field)
        {
            return field.GetValue(true, true);
        }

        private static void ReplaceId(Item sourceItem, Item targetItem, StringBuilder value)
        {
            value.Replace(sourceItem.ID.ToString(), targetItem.ID.ToString());
        }

        private static void ReplaceShortId(Item sourceItem, Item targetItem, StringBuilder value)
        {
            value.Replace(sourceItem.ID.ToShortID().ToString(), targetItem.ID.ToShortID().ToString());
        }

        private static void ReplaceFullPath(Item sourceItem, Item targetItem, StringBuilder value)
        {
            value.Replace(sourceItem.Paths.FullPath, targetItem.Paths.FullPath);
        }

        private static void ReplaceContentPath(Item sourceItem, Item targetItem, StringBuilder value)
        {
            if (sourceItem.Paths.IsContentItem)
            {
                value.Replace(sourceItem.Paths.ContentPath, targetItem.Paths.ContentPath);
            }
        }

        private static void UpdateFieldValue(Field field, string initialValue, string value)
        {
            if (initialValue.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using (new EditContext(field.Item, false, false))
            {
                field.Value = value;
            }
        }
    }
}
