﻿using System;
using System.Data.Services.Client;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.MediaServices.Client.Tests.Common;

namespace Microsoft.WindowsAzure.MediaServices.Client.Tests
{
  public partial class JobTests
  {

      [TestMethod]
      [DeploymentItem(@"Media\SmallWmv.wmv", "Media")]
      [ExpectedException(typeof(DataServiceRequestException))]
      public void ShouldThrowSubmittingJobWhenNonexistingStorageSpecifiedForOutPut()
      {
          try
          {
              IAsset asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.StorageEncrypted);
              IMediaProcessor mediaProcessor = GetMediaProcessor(_mediaContext, WindowsAzureMediaServicesTestConfiguration.MpEncoderName);
              string name = GenerateName("Job 1");
              IJob job = _mediaContext.Jobs.Create(name);
              ITask task = job.Tasks.AddNew("Task1", mediaProcessor, GetWamePreset(mediaProcessor), TaskOptions.None);
              task.InputAssets.Add(asset);
              task.OutputAssets.AddNew("Output asset", Guid.NewGuid().ToString(), AssetCreationOptions.None);
              job.Submit();
          }
          catch (DataServiceRequestException ex)
          {
              Assert.IsTrue(ex.Response.First().Error.Message.Contains("Cannot find the storage account"));
              throw;
          }
      }

      [TestMethod]
      [DeploymentItem(@"Media\SmallWmv.wmv", "Media")]
      public void ShouldSubmitJobWhereOutPutInDefaultStorage()
      {
          IAsset asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.StorageEncrypted);
          IMediaProcessor mediaProcessor = GetMediaProcessor(_mediaContext, WindowsAzureMediaServicesTestConfiguration.MpEncoderName);
          string name = GenerateName("Job 1");
          IJob job = _mediaContext.Jobs.Create(name);
          ITask task = job.Tasks.AddNew("Task1", mediaProcessor, GetWamePreset(mediaProcessor), TaskOptions.None);
          task.InputAssets.Add(asset);
          task.OutputAssets.AddNew("Output asset", _mediaContext.DefaultStorageAccount.Name, AssetCreationOptions.None);
          job.Submit();
          WaitForJob(job.Id, JobState.Finished, VerifyAllTasksFinished);
      }

      
      [TestMethod]
      [DeploymentItem(@"Media\SmallWmv.wmv", "Media")]
      public void ShouldSubmitJobWhereOutPutInNoneDefaultStorage()
      {
          var nondefault = _mediaContext.StorageAccounts.Where(c => c.IsDefault == false).FirstOrDefault();
          //This test need to be executed when media account multiple storage account associated with it. 
          if (nondefault == null)
          {
              return;
          }
          IAsset asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.StorageEncrypted);
          IMediaProcessor mediaProcessor = GetMediaProcessor(_mediaContext, WindowsAzureMediaServicesTestConfiguration.MpEncoderName);
          string name = GenerateName("Job 1");
          IJob job = _mediaContext.Jobs.Create(name);
          ITask task = job.Tasks.AddNew("Task1", mediaProcessor, GetWamePreset(mediaProcessor), TaskOptions.None);
          task.InputAssets.Add(asset);
          var outputAsset = task.OutputAssets.AddNew("Output asset", nondefault.Name, AssetCreationOptions.None);
          job.Submit(); 
          Assert.AreEqual(nondefault.Name,outputAsset.StorageAccountName,"Storage account are not matching");
          WaitForJob(job.Id, JobState.Finished, VerifyAllTasksFinished);   
          
          var refreshed = _mediaContext.Jobs.Where(c => c.Id == job.Id).FirstOrDefault();
          Assert.IsNotNull(refreshed);
          Assert.AreEqual(1,refreshed.Tasks.Count,"Number of Tasks in job is not matching");
          Assert.AreEqual(1, refreshed.Tasks[0].OutputAssets.Count, "Number of output assets in job is not matching");
          Assert.AreEqual(nondefault.Name, refreshed.Tasks[0].OutputAssets[0].StorageAccountName, "Storage account name in output assset is not matching");
          
      }

      
      [TestMethod]
      [DeploymentItem(@"Media\SmallWmv.wmv", "Media")]
      public void ShouldSaveJobAsTemplateAndCreateNewJobwithItWhereOutPutInNoneDefaultStorage()
      {
          var nondefault = _mediaContext.StorageAccounts.Where(c => c.IsDefault == false).FirstOrDefault();

          //This test need to be executed when media account multiple storage account associated with it. 
          if (nondefault == null)
          {
              return;
          }

          IAsset asset = AssetTests.CreateAsset(_mediaContext, _smallWmv, AssetCreationOptions.StorageEncrypted);
          IMediaProcessor mediaProcessor = GetMediaProcessor(_mediaContext, WindowsAzureMediaServicesTestConfiguration.MpEncoderName);
          string name = GenerateName("Job 1");
          IJob job = _mediaContext.Jobs.Create(name);
          ITask task = job.Tasks.AddNew("Task1", mediaProcessor, GetWamePreset(mediaProcessor), TaskOptions.None);
          task.InputAssets.Add(asset);
          var outputAsset = task.OutputAssets.AddNew("Output asset", nondefault.Name, AssetCreationOptions.None);
          job.Submit();
          var template = job.SaveAsTemplate("JobTests.StorageAccounts.cs_" + Guid.NewGuid().ToString());
          string newJobName = GenerateName("Job from template with non default storage account");
          var newJob = _mediaContext.Jobs.Create(newJobName, template, new[] { asset });
          newJob.Submit();
          WaitForJob(newJob.Id, JobState.Finished, VerifyAllTasksFinished);
          newJob = _mediaContext.Jobs.Where(c => c.Id == newJob.Id).FirstOrDefault();
          Assert.AreEqual(nondefault.Name, newJob.Tasks[0].OutputAssets[0].StorageAccountName, "Storage account name in output assset is not matching");


      }

  }
}