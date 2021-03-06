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
		static void Disable()
		{
			try
			{
				//collect all installed fields
				List<ListConfiguration> allFields = SqlRemarQ.GetListConfigurations();
				Console.WriteLine(string.Format("{0} Fields to remove ", allFields.Count));
				if (allFields.Count > 0)
				{
					SharePointListDictionary listDict = new SharePointListDictionary();
					foreach (ListConfiguration listConfig in allFields)
					{
						listDict.Add(listConfig.SiteId, listConfig.WebId, listConfig.ListId);
					}
					foreach (Guid siteId in listDict.SiteIds())
					{
						try
						{
							using (SPSite listSite = new SPSite(siteId))
							{
								Console.WriteLine("Processing site " + listSite.Url.ToString());
								foreach (Guid webId in listDict.WebIds(siteId))
								{
									try
									{
										using (SPWeb listWeb = listSite.AllWebs[webId])
										{
											Console.WriteLine("Processing web " + listWeb.Title);
											foreach (Guid listId in listDict.ListIds(siteId, webId))
											{
												try
												{
													SPList fixMe = listWeb.Lists[listId];
													Console.WriteLine("Un-RemarQ-ing " + fixMe.Title);
													List<ReadUnreadField> fieldsToRemove = BalsamicSolutions.ReadUnreadSiteColumn.Utilities.FindFieldsOnList(fixMe);
													foreach (ReadUnreadField removeMe in fieldsToRemove)
													{
														removeMe.Delete();
													}
												}
												catch (ArgumentException)
												{
													Console.WriteLine("A missing list has been removed for " + listId.ToString("B"));
												}
											}
										}
									}
									catch (ArgumentException)
									{
										Console.WriteLine("A missing web has been removed for " + webId.ToString("B"));
									}
								}
							}
						}
						catch (FileNotFoundException)
						{
							Console.WriteLine("A missing site has been removed for " + siteId.ToString("B"));
						}
					}
					Console.WriteLine("Processing cleanup");
				}
			}
			finally
			{
				EndProgress();
			}
			RunJobs();
			foreach (ListConfiguration listConfig in SqlRemarQ.GetListConfigurations())
			{
				SqlRemarQ.DeProvisionReadUnreadTableAndRemoveConfigurationEntry(listConfig.ListId);
			}
		}
	}
}