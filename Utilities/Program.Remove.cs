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
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using BalsamicSolutions.ReadUnreadSiteColumn;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace Utilities
{
	partial class Program
	{
		static void Remove()
		{
			Console.WriteLine("Collecting farm topology and qualifiying lists");
			 
			SharePointListDictionary allListsInFarm = new SharePointListDictionary();
			SPWebService webSvc = SPFarm.Local.Services.GetValue<SPWebService>(string.Empty);
			foreach (SPWebApplication webApp in webSvc.WebApplications)
			{
				foreach (SPSite webSite in webApp.Sites)
				{
					if (RemarQIsActive(webSite))
					{
						foreach (SPWeb siteWeb in webSite.AllWebs)
						{
							foreach (SPList webList in siteWeb.Lists)
							{
								allListsInFarm.Add(webSite.ID, siteWeb.ID, webList.ID);
								Console.Write("+");
							}
						}
					}
				}
			}
			Remove(allListsInFarm);
			FarmSettings.Remove();
		}

		static void Remove(SharePointListDictionary listDict)
		{
			Guid featureGuid = new Guid("830c7e22-415b-4efb-bee3-407fa24faeb1");
			int jobCount = 0;
			StartProgress();
			foreach (Guid siteId in listDict.SiteIds())
			{
				try
				{
					using (SPSite listSite = new SPSite(siteId))
					{
						foreach (Guid webId in listDict.WebIds(siteId))
						{
							try
							{
								using (SPWeb listWeb = listSite.AllWebs[webId])
								{
									foreach (Guid listId in listDict.ListIds(siteId, webId))
									{
										try
										{
											SPList webList = listWeb.Lists[listId];
											List<ReadUnreadField> foundFields = BalsamicSolutions.ReadUnreadSiteColumn.Utilities.FindFieldsOnList(webList);
											if (foundFields.Count > 0)
											{
												jobCount++;
												EndProgress();
												try
												{
													SqlRemarQ.DeProvisionReadUnreadTableAndRemoveConfigurationEntry(webList.ID);
												}
												catch (Exception sqlError)
												{
													Console.WriteLine("Error removing RemarQ from " + webList.Title);
													Console.WriteLine(sqlError.Message);
												}
												Console.WriteLine("Removing RemarQ from " + webList.Title);
												foreach (ReadUnreadField foundField in foundFields)
												{
													try
													{
														//foundField.Delete();
														webList.Fields.Delete(foundField.InternalName);
													}
													catch (Exception fieldError)
													{
														Console.WriteLine("Error removing RemarQ from " + webList.Title);
														Console.WriteLine(fieldError.Message);
													}
												}
												
												StartProgress();
											}
										}
										catch (ArgumentException)
										{
										}
									}
								}
							}
							catch (ArgumentException)
							{
							}
						}
						if (RemarQIsActive(listSite))
						{
							listSite.RootWeb.Features.Remove(featureGuid);
							listSite.RootWeb.Update();
						}
					}
				}
				catch (FileNotFoundException)
				{
				}
			}
			EndProgress();
			Console.WriteLine(string.Format("Removed {0} RemarQ fields", jobCount));
			RunJobs();
		}
	}
}