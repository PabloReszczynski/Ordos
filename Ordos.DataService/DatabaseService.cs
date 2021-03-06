﻿using Ordos.DataService.Data;
using Ordos.Core.Models;
using Ordos.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ordos.DataService
{
    public static class DatabaseService
    {
        private static readonly NLog.Logger Logger = Core.Utilities.Logger.Init();

        public static List<Device> Devices { get; private set; }
        public static string CompanyName { get; set; }
        public static string CompanyNameLabel { get; } = "CompanyName";

        public static void Init()
        {
            LoadDevices();
            LoadApplicationSettings();
        }

        public static void LoadDevices()
        {
            using (var context = new SystemContext())
            {
                //No changes, no tracking
                Devices = context
                            .Devices.AsNoTracking()
                            .Include(d => d.DisturbanceRecordings).AsNoTracking()
                            .ToList();
            }
        }

        public static void LoadApplicationSettings()
        {
            using (var context = new SystemContext())
            {
                if (context.ConfigurationValues == null)
                    return;

                //No changes, no tracking
                CompanyName = context
                                .ConfigurationValues.AsNoTracking()
                                .FirstOrDefault(x => x.Id.Contains(CompanyNameLabel))?
                                .Value;
            }
        }

        public static void UpdateIEDConnectionStatus(Device device, bool isConnected)
        {
            try
            {
                using (var context = new SystemContext())
                {
                    //As tracking, updated based on connectivity
                    var dev = context
                                .Devices
                                .FirstOrDefault(x => x.Id.Equals(device.Id));

                    if (dev == null) return;

                    dev.IsConnected = isConnected;
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            Logger.Trace($"{device.FullName} - {device.IPAddress} - Connection {(isConnected ? "Successful" : "Failed")}");
        }

        public static void StoreComtradeFilesToDatabase(Device device, List<DisturbanceRecording> comtradeFiles)
        {
            using (var context = new SystemContext())
            {
                //Get the DB device:
                //Tracking, DRs will get updated.
                var dev = context
                            .Devices
                            .Include(ied => ied.DisturbanceRecordings)
                            .ThenInclude(dr => dr.DRFiles)
                            .FirstOrDefault(x => x.Id.Equals(device.Id));

                //If device not found, return empty list:
                if (dev == null)
                {
                    Logger.Error($"{device} Not found on the DB");
                    return;
                }

                foreach (var item in comtradeFiles)
                {
                    Logger.Trace($"{device} - {item}");
                    item.DeviceId = device.Id;
                    dev.DisturbanceRecordings.Add(item);
                }

                context.SaveChanges();
            }
        }

        public static void StoreIEDFilesToDatabase(Device device, IEnumerable<IEDFile> iedFiles)
        {
            using (var context = new SystemContext())
            {
                //Get the DB device:
                //Tracking, DRs will get updated.
                var dev = context
                    .Devices
                    .Include(ied => ied.IEDFiles)
                    .FirstOrDefault(x => x.Id.Equals(device.Id));

                //If device not found, return empty list:
                if (dev == null)
                {
                    Logger.Error($"{device} Not found on the DB");
                    return;
                }

                foreach (var item in iedFiles)
                {
                    Logger.Trace($"{device} - {item}");
                    item.DeviceId = device.Id;
                    dev.IEDFiles.Add(item);
                }

                context.SaveChanges();
            }
        }

        public static IEnumerable<IEDFile> FilterExistingFiles(Device device, IEnumerable<IEDFile> downloadableFileList)
        {
            var filteredDownloadableFileList = new List<IEDFile>();

            using (var context = new SystemContext())
            {
                //Get the DB device:
                var dev = context.Devices
                                    .Include(x=>x.IEDFiles)
                                    .FirstOrDefault(x => x.Id.Equals(device.Id));

                //If device not found, return empty list:
                if (dev == null)
                {
                    Logger.Error($"{device} Not found on the DB");
                    return filteredDownloadableFileList;
                }

                //Get the list of all DRFiles in the downloadableFileList:
                //If the ied already has that file (file.name && file.size) (should have it in the database), skip;
                //otherwise add that file to the filtered download list:
                var drFiles = dev.IEDFiles;

                foreach (var downloadableFile in downloadableFileList)
                {
                    Logger.Trace($"{device} - {downloadableFile}");

                    if (drFiles.Any(x => x.FileName.Equals(downloadableFile.FileName)
                                      && x.FileSize.Equals(downloadableFile.FileSize)))
                    {
                        //It seems like the logs on the 670s never hit this code:
                        Logger.Trace($"{device} - File already in the DB");
                        continue;
                    }
                    Logger.Trace($"{device} - New file found!");
                    Logger.Trace($"Filename: {downloadableFile.FileName} DestinationFilename: {downloadableFile.FileName.GetDestinationFilename()} Filesize:{downloadableFile.FileSize}");

                    filteredDownloadableFileList.Add(downloadableFile);
                }
            }
            return filteredDownloadableFileList;
        }
    }
}
