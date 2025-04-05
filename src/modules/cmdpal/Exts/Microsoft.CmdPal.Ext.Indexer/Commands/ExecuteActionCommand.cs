// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CmdPal.Ext.Indexer.Data;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.AI.Actions;
using Windows.AI.Actions.Hosting;

namespace Microsoft.CmdPal.Ext.Indexer.Commands;

internal sealed partial class ExecuteActionCommand : InvokableCommand
{
    private readonly IndexerItem _item;
    private readonly ActionRuntime actionRuntime;
    private readonly ActionDefinition action;
    private readonly ActionOverload overload;

    internal ExecuteActionCommand(IndexerItem item, ActionRuntime actionRuntime, ActionDefinition action, ActionOverload overload)
    {
        this._item = item;
        this.actionRuntime = actionRuntime;
        this.action = action;
        this.overload = overload;
        this.Name = overload.DescriptionTemplate;
        this.Icon = new IconInfo(action.IconFullPath);
    }

    public override CommandResult Invoke()
    {
        var task = Task.Run(InvokeAsync);
        task.Wait();

        return task.Result;
    }

    private async Task<CommandResult> InvokeAsync()
    {
        var invocationContext = actionRuntime?.CreateInvocationContext(action.Id);
        if (invocationContext is null)
        {
            return CommandResult.ShowToast("Failed to create invocation context for action " + action.Id);
        }

        if (overload.GetInputs().Length != 1)
        {
            return CommandResult.ShowToast("Action " + action.Id + " has multiple inputs, which is not supported.");
        }

        var input = overload.GetInputs()[0];
        SetInput(invocationContext, input);

        try
        {
            await overload.InvokeAsync(invocationContext);
            return CommandResult.GoHome();
        }
        catch (Exception ex)
        {
            return CommandResult.ShowToast("Failed to invoke action " + action.Id + ": " + ex.Message);
        }
    }

    private void SetInput(ActionInvocationContext invocationContext, ActionEntityRegistrationInfo input)
    {
        if (input.Kind == ActionEntityKind.Photo)
        {
            var photoEntity = actionRuntime.EntityFactory.CreatePhotoEntity(_item.FullPath);
            invocationContext.SetInputEntity(input.Name, photoEntity);
        }
        else if (input.Kind == ActionEntityKind.Document)
        {
            var documentEntity = actionRuntime.EntityFactory.CreateDocumentEntity(_item.FullPath);
            invocationContext.SetInputEntity(input.Name, documentEntity);
        }
        else if (input.Kind == ActionEntityKind.File)
        {
            var fileEntity = actionRuntime.EntityFactory.CreateFileEntity(_item.FullPath);
            invocationContext.SetInputEntity(input.Name, fileEntity);
        }
    }
}
