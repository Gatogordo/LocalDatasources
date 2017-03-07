using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using TheReference.DotNet.Sitecore.LocalDatasources.Services;

namespace TheReference.DotNet.Sitecore.LocalDatasources.Infrastructure.Events
{
    public class ItemLocalDatasourcesHandler
    {
        public void OnItemCopied(object sender, EventArgs args)
        {
            var sourceItem = Event.ExtractParameter(args, 0) as Item;
            if (sourceItem == null)
            {
                return;
            }

            var targetItem = Event.ExtractParameter(args, 1) as Item;
            if (targetItem == null)
            {
                return;
            }

            LocalDatasourceService.UpdateTree(sourceItem, targetItem);
        }

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

            LocalDatasourceService.UpdateTree(item.Branch.InnerItem.Children.FirstOrDefault(), item);
        }
    }
}
