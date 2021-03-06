﻿// -----------------------------------------------------------------------------
//This is free and unencumbered software released into the public domain.
//Anyone is free to copy, modify, publish, use, compile, sell, or
//distribute this software, either in source code form or as a compiled
//binary, for any purpose, commercial or non-commercial, and by any
//means.
//In jurisdictions that recognize copyright laws, the author or authors
//of this software dedicate any and all copyright interest in the
//software to the public domain.We make this dedication for the benefit
//of the public at large and to the detriment of our heirs and
//successors.We intend this dedication to be an overt act of
//relinquishment in perpetuity of all present and future rights to this
//software under copyright law.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
//OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
//ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.
//For more information, please refer to<http://unlicense.org>
// ----------------------------------------------------------------------------- 

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Security.AntiXss;
using System.Web.UI.WebControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.ApplicationPages;
using Microsoft.SharePoint.ApplicationPages.WebControls;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;

namespace BalsamicSolutions.ReadUnreadSiteColumn.Administration
{
	public partial class ReadUnreadFarmSettingsEditorAdvanced : OperationsPage
	{ 
		protected void Page_Load(object sender, EventArgs e)
		{ 
			this.FarmSettingsCDNDescription.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsCDNDescription");
			this.PropEditRefreshIntervalDescription.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"PropEditRefreshIntervalDescription");
			this.FarmSettingsRefreshIntervalDescription.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsRefreshIntervalDescription");
			this.txtJQueryPathLabel.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsJQueryPathLabel");
			this.txtKendoScriptLabel.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsKendoScriptPathLabel");
			this.txtKendoStyePathLabel.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsKendoStylePathLabel");
			this.txtKendoThemePathLabel.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsKendoThemeLabel");

			this.FarmSettingsTrackDocumentsDescriptionLiteral.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsTrackDocumentsDescription");
			this.FarmSettingsChkDocumentTrackingLabelLiteral.Text = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture,"FarmSettingsChkDocumentTrackingLabel");

			if (SPFarm.Local.CurrentUserIsAdministrator())
			{
				this.btnApply.Click += this.BtnApply_Click;
				if (!this.Page.IsPostBack)
				{
					FarmSettings farmSettings = null;
					try
					{
						farmSettings = (FarmSettings)SPFarm.Local.GetObject(FarmSettings.SettingsId);
					}
					catch (InvalidCastException)
					{
						RemarQLog.LogMessage("Invalid or missing configuration object");
						farmSettings = null;
					}
                  
					if (null != farmSettings)
					{
						this.txtJQueryPath.Text = farmSettings.JQueryPath;
						this.txtKendoScriptPath.Text = farmSettings.KendoJavasScriptPath;
						this.txtKendoStyePath.Text = farmSettings.KendoStyleCommonPath;
						this.txtKendoThemePath.Text = farmSettings.KendoStyleThemePath;
						this.txtRefreshInterval.Text = farmSettings.MinJavascriptClientRefreshInterval.ToString(CultureInfo.InvariantCulture);
						this.chkDocumentTracking.Checked = farmSettings.TrackDocuments;
					}
				}
			}
		}

		void BtnApply_Click(object sender, EventArgs e)
		{ 
			FarmSettings farmSettings = null;
			try
			{
				farmSettings = (FarmSettings)SPFarm.Local.GetObject(FarmSettings.SettingsId);
			}
			catch (InvalidCastException)
			{
				RemarQLog.LogMessage("Invalid or missing configuration object");
				farmSettings = null;
			}
			if (null == farmSettings)
			{
				farmSettings = new FarmSettings();
			}
			farmSettings.TrackDocuments = this.chkDocumentTracking.Checked;
			farmSettings.JQueryPath = this.txtJQueryPath.Text ;
			farmSettings.KendoJavasScriptPath = this.txtKendoScriptPath.Text ;
			farmSettings.KendoStyleCommonPath = this.txtKendoStyePath.Text ;
			farmSettings.KendoStyleThemePath = this.txtKendoThemePath.Text ;
			int refreshInterval = 0;
			if(int.TryParse(this.txtRefreshInterval.Text,out refreshInterval))
			{
				if(refreshInterval<0) refreshInterval = 0;
				farmSettings.MinJavascriptClientRefreshInterval = refreshInterval;
			}
			if (FarmLicense.License.LicenseMode < LicenseModeType.Professional)
			{
				farmSettings.TrackDocuments = false;
			}

			try
			{
				farmSettings.Update();
				if (farmSettings.TrackDocuments)
				{
					ReadUnreadHttpModule.RegisterModule(false);
				}
				else
				{
					ReadUnreadHttpModule.UnregisterModule(false);
				}
				this.Page.Response.Clear();
				this.Page.Response.Write(Constants.CloseSharePointDialogScript);
				this.Page.Response.End();
			}
			catch (SPException saveError)
			{
				RemarQLog.LogError(saveError);
				this.btnApply.Enabled = false;
			}
		}
	}
}