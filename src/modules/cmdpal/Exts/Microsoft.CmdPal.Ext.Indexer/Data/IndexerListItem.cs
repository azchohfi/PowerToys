// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.CmdPal.Ext.Indexer.Commands;
using Microsoft.CmdPal.Ext.Indexer.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation.Metadata;

namespace Microsoft.CmdPal.Ext.Indexer.Data;

internal sealed partial class IndexerListItem : ListItem
{
    internal string FilePath { get; private set; }

    public IndexerListItem(
        IndexerItem indexerItem,
        IncludeBrowseCommand browseByDefault = IncludeBrowseCommand.Include)
        : base(new OpenFileCommand(indexerItem))
    {
        FilePath = indexerItem.FullPath;

        Title = indexerItem.FileName;
        Subtitle = indexerItem.FullPath;
        List<CommandContextItem> context = [];
        if (indexerItem.IsDirectory())
        {
            var directoryPage = new DirectoryPage(indexerItem.FullPath);
            if (browseByDefault == IncludeBrowseCommand.AsDefault)
            {
                // Swap the open file command into the context menu
                context.Add(new CommandContextItem(Command));
                Command = directoryPage;
            }
            else if (browseByDefault == IncludeBrowseCommand.Include)
            {
                context.Add(new CommandContextItem(directoryPage));
            }
        }

        List<CommandContextItem> actions = [];

        string extension = System.IO.Path.GetExtension(indexerItem.FullPath).ToLower(CultureInfo.InvariantCulture);

        if (extension != null && ApiInformation.IsApiContractPresent("Windows.AI.Actions.ActionsContract", 1))
        {
            var actionRuntime = ActionRuntimeFactory.CreateActionRuntime();

            var availableActions = actionRuntime.ActionCatalog.GetAllActions();

            foreach (var action in availableActions)
            {
                var overloads = action.GetOverloads();
                foreach (var overload in overloads)
                {
                    var inputs = overload.GetInputs();

                    // Check if the overload has a single input
                    if (inputs.Length != 1)
                    {
                        continue;
                    }

                    foreach (var input in inputs)
                    {
                        if (((extension == ".jpg" || extension == ".jpeg" || extension == ".png") &&
                                input.Kind == global::Windows.AI.Actions.ActionEntityKind.Photo) ||
                            ((extension == ".docx" || extension == ".doc" || extension == ".pdf" || extension == ".txt") &&
                                input.Kind == global::Windows.AI.Actions.ActionEntityKind.Document) ||
                            input.Kind == global::Windows.AI.Actions.ActionEntityKind.File)
                        {
                            actions.Add(new CommandContextItem(new ExecuteActionCommand(indexerItem, actionRuntime, action, overload)));
                            break;
                        }
                    }
                }
            }
        }

        MoreCommands = [
            ..context,
            new CommandContextItem(new OpenWithCommand(indexerItem)),
            ..actions,
            new CommandContextItem(new ShowFileInFolderCommand(indexerItem.FullPath) { Name = Resources.Indexer_Command_ShowInFolder }),
            new CommandContextItem(new CopyPathCommand(indexerItem)),
            new CommandContextItem(new OpenInConsoleCommand(indexerItem)),
            new CommandContextItem(new OpenPropertiesCommand(indexerItem)),
        ];
    }
}

internal enum IncludeBrowseCommand
{
    AsDefault = 0,
    Include = 1,
    Exclude = 2,
}
