﻿using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using AMF.Common;
using AMF.Common.ViewModels;
using AMF.Parser;
using AMF.Tools.Properties;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using MuleSoft.RAML.Tools;

namespace AMF.Tools
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class EditPropertiesCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("0fd6b089-7a11-4828-ba12-f5bea9e4a489");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;
        private CommandID menuCommandID;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditPropertiesCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private EditPropertiesCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                menuItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static EditPropertiesCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new EditPropertiesCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void MenuItemCallback(object sender, EventArgs e)
        {
            ChangeCommandStatus(menuCommandID, false);

            // Get the file path
            uint itemid;
            IVsHierarchy hierarchy;
            if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;
            string ramlFilePath;
            ((IVsProject)hierarchy).GetMkDocument(itemid, out ramlFilePath);

            var refFilePath = InstallerServices.GetRefFilePath(ramlFilePath);


            var editorModel = new RamlPropertiesEditorViewModel();
            editorModel.Load(refFilePath, Settings.Default.ContractsFolderName, Settings.Default.ApiReferencesFolderName);
            AmfToolsPackage.WindowManager.ShowDialog(editorModel);

            //var frm = new RamlPropertiesEditor();
            //frm.Load(refFilePath, Settings.Default.ContractsFolderName, Settings.Default.ApiReferencesFolderName);
            //var result = frm.ShowDialog();
            if (editorModel.WasSaved)
            {

                if (IsServerSide(ramlFilePath))
                {
                    var ramlScaffoldUpdater = RamlScaffoldServiceBase.GetRamlScaffoldService(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider);
                    ramlScaffoldUpdater.UpdateRaml(ramlFilePath);
                }
                else
                {
                    var templatesManager = new TemplatesManager();
                    var ramlFolder = Path.GetDirectoryName(ramlFilePath).TrimEnd(Path.DirectorySeparatorChar);
                    var generatedFolderPath = ramlFolder.Substring(0, ramlFolder.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    if (!templatesManager.ConfirmWhenIncompatibleClientTemplate(generatedFolderPath))
                        return;

                    await RegenerateClientCode(ramlFilePath);
                }
            }

            ChangeCommandStatus(menuCommandID, true);
        }

        private async System.Threading.Tasks.Task RegenerateClientCode(string ramlFilePath)
        {
            var ramlInfo = await RamlInfoService.GetRamlInfo(ramlFilePath);

            //TODO:
            //var result = RamlReferenceServiceBase.GetRamlReferenceService().GenerateCode(ramlInfo, proj,, GetExtensionPath());
            //if (!result.IsSuccess)
            //{
            //    ActivityLog.LogError(VisualStudioAutomationHelper.RamlVsToolsActivityLogSource, result.ErrorMessage);
            //    MessageBox.Show(result.ErrorMessage);
            //}
        }

        private string GetExtensionPath()
        {
            var extensionPath = Path.GetDirectoryName(GetType().Assembly.Location) + Path.DirectorySeparatorChar;
            return extensionPath;
        }

        private bool IsServerSide(string ramlFilePath)
        {
            if (ramlFilePath.Contains(Settings.Default.ContractsFolderName) && !ramlFilePath.Contains(Settings.Default.ApiReferencesFolderName))
                return true;

            if (!ramlFilePath.Contains(Settings.Default.ContractsFolderName) && ramlFilePath.Contains(Settings.Default.ApiReferencesFolderName))
                return false;

            throw new InvalidOperationException("Cannot determine if the raml is used on the server or the client");
        }

        private void ChangeCommandStatus(CommandID commandId, bool enable)
        {
            var mcs = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs == null) return;

            var menuCmd = mcs.FindCommand(commandId);
            if (menuCmd != null) menuCmd.Enabled = enable;
        }

        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;

            var monitorSelection = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                IVsMultiItemSelect multiItemSelect;
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    // there is no selection
                    return false;
                }

                // multiple items are selected
                if (multiItemSelect != null) return false;

                // there is a hierarchy root node selected, thus it is not a single item inside a project

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectId;

                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectId)))
                {
                    return false; // hierarchy is not a project inside the Solution if it does not have a ProjectID Guid
                }

                // if we got this far then there is a single project item selected
                return true;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
    }
}
