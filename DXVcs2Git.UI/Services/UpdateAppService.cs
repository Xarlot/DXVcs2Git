using System;
using System.Deployment.Application;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;

namespace DXVcs2Git.UI.Services {
    public static class UpdateAppService {
        public static void Update(IMessageBoxService messageBoxService) {
            UpdateCheckInfo info = null;

            if(ApplicationDeployment.IsNetworkDeployed) {
                ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                try {
                    info = ad.CheckForDetailedUpdate();

                }
                catch(DeploymentDownloadException dde) {
                    messageBoxService?.ShowMessage("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                    return;
                }
                catch(InvalidDeploymentException ide) {
                    messageBoxService?.ShowMessage("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                    return;
                }
                catch(InvalidOperationException ioe) {
                    messageBoxService?.ShowMessage("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                    return;
                }
                if(!info.UpdateAvailable) {
                    messageBoxService?.ShowMessage("No updates available", "No updates available");
                    return;
                }
                Boolean doUpdate = true;
                if(!info.IsUpdateRequired) {
                    MessageResult? dr = messageBoxService?.ShowMessage("An update is available. Would you like to update the application now?", "Update Available", MessageButton.OKCancel);
                    if(dr != MessageResult.OK)
                        doUpdate = false;
                }
                else {
                    // Display a message that the app MUST reboot. Display the minimum required version.
                    messageBoxService?.ShowMessage("This application has detected a mandatory update from your current " + "version to version " +
                        info.MinimumRequiredVersion.ToString() +
                        ". The application will now install the update and restart.",
                        "Update Available", MessageButton.OK,
                        MessageIcon.Information);
                }

                if(doUpdate) {
                    try {
                        ad.Update();
                        messageBoxService?.ShowMessage("The application has been upgraded, and will now restart.");
                        System.Windows.Application.Current.Shutdown();
                        System.Windows.Forms.Application.Restart();
                    }
                    catch(DeploymentDownloadException dde) {
                        messageBoxService?.ShowMessage("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                        return;
                    }
                }
            }
        }
    }
}
