using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Sitecore.Pipelines.GetPlaceholderRenderings;
using Sitecore.Pipelines.GetRenderingDatasource;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using WebEditUtil = Sitecore.Web.WebEditUtil;

namespace TheReference.DotNet.Sitecore.LocalDatasources.Infrastructure.Commands
{
    [Serializable]
    public class AddRendering : global::Sitecore.Shell.Applications.WebEdit.Commands.AddRendering
    {
        protected new static void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (!args.HasResult)
                {
                    return;
                }

                if (!IsSelectDatasourceDialogPostBack(args))
                {
                    string itemPath;
                    bool flag;
                    if (args.Result.IndexOf(',') >= 0)
                    {
                        var strArray = args.Result.Split(',');
                        itemPath = strArray[0];
                        flag = strArray[2] == "1";
                    }
                    else
                    {
                        itemPath = args.Result;
                        flag = false;
                    }

                    var itemNotNull = Client.GetItemNotNull(itemPath);
                    var renderingDatasourceArgs = new GetRenderingDatasourceArgs(itemNotNull)
                                                      {
                                                          ContextItemPath = args.Parameters["contextitempath"],
                                                          ContentLanguage = WebEditUtil.GetClientContentLanguage()
                                                      };
                    var str = itemNotNull.ID.ToShortID().ToString();
                    if (!IsMorphRenderingsRequest(args))
                    {
                        CorePipeline.Run("getRenderingDatasource", renderingDatasourceArgs);
                    }

                    if (!string.IsNullOrEmpty(renderingDatasourceArgs.DialogUrl) && !IsMorphRenderingsRequest(args))
                    {
                        if (!string.IsNullOrEmpty(renderingDatasourceArgs.CurrentDatasource))
                        {
                            var localDatasourceItem = Client.ContentDatabase.GetItem(renderingDatasourceArgs.CurrentDatasource);
                            WebEditResponse.Eval(FormattableString.Invariant($"Sitecore.PageModes.ChromeManager.handleMessage('chrome:placeholder:controladded', {{ id: '{itemNotNull.ID.Guid.ToString("N").ToUpperInvariant()}', openProperties: {flag.ToString().ToLowerInvariant()}, dataSource: '{localDatasourceItem.ID.Guid.ToString("B").ToUpperInvariant()}' }});"));
                        }
                        else
                        {
                            args.IsPostBack = false;
                            args.Parameters["SelectedRendering"] = str;
                            args.Parameters["OpenProperties"] = flag.ToString().ToLowerInvariant();
                            SheerResponse.ShowModalDialog(renderingDatasourceArgs.DialogUrl, "1200px", "700px", string.Empty, true);
                            args.WaitForPostBack();
                        }
                    }
                    else
                    {
                        WebEditResponse.Eval(FormattableString.Invariant($"Sitecore.PageModes.ChromeManager.handleMessage('{(IsMorphRenderingsRequest(args) ? "chrome:rendering:morphcompleted" : "chrome:placeholder:controladded")}', {{ id: '{str}', openProperties: {flag.ToString().ToLowerInvariant()} }});"));
                    }
                }
                else
                {
                    WebEditResponse.Eval(FormattableString.Invariant($"Sitecore.PageModes.ChromeManager.handleMessage('chrome:placeholder:controladded', {{ id: '{args.Parameters["SelectedRendering"] ?? string.Empty}', openProperties: {args.Parameters["OpenProperties"] ?? "false"}, dataSource: '{args.Result}' }});"));
                }
            }
            else
            {
                List<Item> placeholderRenderings;
                string dialogUrl;
                RunGetPlaceholderRenderingsPipeline(args.Parameters, out placeholderRenderings, out dialogUrl);
                if (string.IsNullOrEmpty(dialogUrl))
                {
                    return;
                }

                SheerResponse.ShowModalDialog(dialogUrl, "720px", "470px", string.Empty, true);
                args.WaitForPostBack();
            }
        }

        private static bool IsMorphRenderingsRequest(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            return args.Parameters["renderingIds"] != null;
        }

        private static bool IsSelectDatasourceDialogPostBack(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack && args.Parameters["SelectedRendering"] != null)
            {
                return args.Parameters["OpenProperties"] != null;
            }

            return false;
        }

        private static void RunGetPlaceholderRenderingsPipeline(NameValueCollection context, out List<Item> placeholderRenderings, out string dialogUrl)
        {
            Assert.IsNotNull(context, "context");
            var deviceId = ShortID.DecodeID(WebUtil.GetFormValue("scDeviceID"));
            var placeholderRenderingsArgs = new GetPlaceholderRenderingsArgs(context["placeholder"], context["layout"], Client.ContentDatabase, deviceId)
                                                {
                                                    OmitNonEditableRenderings = true
                                                };
            if (!string.IsNullOrEmpty(context["renderingIds"]))
            {
                var list = context["renderingIds"].Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).Select(ID.Parse).ToList();
                placeholderRenderingsArgs.PredefinedRenderingIds = list;
                placeholderRenderingsArgs.Options.Title = "Select a replacement rendering";
                placeholderRenderingsArgs.Options.Icon = "ApplicationsV2/32x32/replace2.png";
            }

            CorePipeline.Run("getPlaceholderRenderings", placeholderRenderingsArgs);
            placeholderRenderings = placeholderRenderingsArgs.PlaceholderRenderings;
            dialogUrl = placeholderRenderingsArgs.DialogURL;
        }
    }
}