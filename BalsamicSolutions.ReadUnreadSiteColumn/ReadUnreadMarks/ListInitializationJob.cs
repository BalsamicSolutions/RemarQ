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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Office.Server.Utilities;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;

namespace BalsamicSolutions.ReadUnreadSiteColumn
{
	/// <summary>
	/// this job runs on demand, it is scheduled by the InitializationQueueMonitorJob 
	/// it expects to empty the work queue of lists to intitialze
	/// it picks it up and processes lists using using the Content
	/// iterator instead of the item query. Its a bit slower but 
	/// wont kick off a throttling problem or unduly load the server
	/// </summary>
	[Guid("4DC0BAE4-9F5B-4D80-BA2C-4D64E3C1E801")]
	public class ListInitializationJob : SPJobDefinition
	{
		public const string JOB_NAME = "RemarQListInitializationJob";

		/// <summary>
		/// use this CTOR
		/// </summary>
		/// <param name="jobName"></param>
		public ListInitializationJob(string jobName)
			: base(jobName, SPAdministrationWebApplication.Local, null, SPJobLockType.Job)
		{
			//if for some reason our title comes back missing we have to provide one
			//otherwise the job status page will throw an error
			string jobTitle = Framework.ResourceManager.GetString(CultureInfo.CurrentUICulture, "BalsamicSolutionsListInitializationJobTitle");
			if (string.IsNullOrWhiteSpace(jobTitle) || jobTitle.IndexOf("ERROR", 0, jobTitle.Length, StringComparison.OrdinalIgnoreCase) > -1)
			{
				jobTitle = "RemarQ List Initialization";
			}
	 
			this.Title = jobTitle;
		}

		/// <summary>
		/// required for serilization
		/// </summary>
		public ListInitializationJob()
			: base()
		{
		}
		
		/// <summary>
		/// execute the job
		/// </summary>
		/// <param name="targetInstanceId"></param>
		public override void Execute(Guid targetInstanceId)
		{
			RemarQLog.LogMessage("Entering BalsamicSolutions.ReadUnreadSiteColumn.ListInitializationJob.Execute");  
			FarmLicense.ExpireCachedObject();
			FarmSettings.ExpireCachedObject();
		 
			using (SPMonitoredScope spScope = new SPMonitoredScope(JOB_NAME))
			{
				this.UpdateProgress(0);
				FarmSettings farmSettings = FarmSettings.Settings;
				if (null != farmSettings && farmSettings.IsOk && farmSettings.Activated && !farmSettings.ExternalJobManager && FarmLicense.License.IsLicensed())
				{
					ListInitializationProcessor batchProcessor = new ListInitializationProcessor(Constants.ReadUnreadQueueTableName,JOB_NAME,this);
					batchProcessor.Execute(); 
					int numComplete = batchProcessor.JobCount;
					System.Diagnostics.Trace.WriteLine(numComplete);
				}
				this.UpdateProgress(100);
			}
			RemarQLog.LogMessage("Exiting Execute BalsamicSolutions.ReadUnreadSiteColumn.ListInitializationJob.Execute");  
		}

		public static bool RunJobNow()
		{
			bool returnValue = false;
			foreach (SPJobDefinition spJob in SPAdministrationWebApplication.Local.JobDefinitions)
			{
				if (spJob.Name == JOB_NAME)
				{
					spJob.RunNow();
					returnValue = true;
					break;
				}
			}
			return returnValue;
		}

		/// <summary>
		/// install this job in the admin site
		/// </summary>
		internal static void Install()
		{
			UnInstall();
			ListInitializationJob spJob = new ListInitializationJob(JOB_NAME);
			//we schedule this yearly so it stays in the scheduled jobs list
			//the queue monitor simply calls RunNow on it
			DateTime nextMonth = DateTime.Now.AddMonths(2); 
			SPYearlySchedule jobSchedule = new SPYearlySchedule();
			jobSchedule.BeginDay = 1;
			jobSchedule.BeginMonth = nextMonth.Month;
			jobSchedule.BeginSecond = 1;
			jobSchedule.BeginHour = 1;
			jobSchedule.BeginMinute = 1;
			jobSchedule.EndDay = 10;
			jobSchedule.EndHour = 5;
			jobSchedule.EndMinute = 59;
			jobSchedule.EndMonth = 1;
			jobSchedule.EndSecond = 59;
			//SPOneTimeSchedule jobSchedule = new SPOneTimeSchedule(DateTime.Now);
			spJob.Schedule = jobSchedule;
			spJob.Update();
		}

		/// <summary>
		/// remove this job from the farm
		/// </summary>
		public static void UnInstall()
		{
			List<SPJobDefinition> jobsToDelete = new List<SPJobDefinition>();
			foreach (SPJobDefinition spJob in SPAdministrationWebApplication.Local.JobDefinitions)
			{
				if (spJob.Name == JOB_NAME)
				{
					jobsToDelete.Add(spJob);
				}
			}
			 
			foreach (SPJobDefinition spJob in jobsToDelete)
			{
				spJob.Delete();
			}
		}
	 
		public static bool IsInstalled()
		{
			foreach (SPJobDefinition spJob in SPAdministrationWebApplication.Local.JobDefinitions)
			{
				if (spJob.Name == JOB_NAME)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsRunning
		{
			get
			{
				foreach (SPRunningJob spJob in SPAdministrationWebApplication.Local.RunningJobs)
				{
					if (null != spJob.JobDefinition && null != spJob.JobDefinition.Name)
					{
						if (spJob.JobDefinition.Name == JOB_NAME)
						{
							return true;
						}
					}
				}

				return false;
			}
		}
	}
}