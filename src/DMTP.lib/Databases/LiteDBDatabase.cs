﻿using System;
using System.Collections.Generic;
using System.Linq;
using DMTP.lib.Databases.Base;
using DMTP.lib.Databases.Tables;
using LiteDB;
using Newtonsoft.Json;

namespace DMTP.lib.Databases
{
    public class LiteDBDatabase : IDatabase
    {
        private const string DbFilename = "data.db";

        public bool DeleteJob(Guid id)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<Jobs>().Delete(a => a.ID == id) > 0;
            }
        }

        public bool AddJob(Jobs item)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<Jobs>().Insert(item) != null;
            }
        }

        public bool UpdateJob(Jobs item)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<Jobs>().Update(item);
            }
        }

        public Jobs GetJob(Guid id)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<Jobs>().FindOne(a => a.ID == id);
            }
        }

        public List<Jobs> GetJobs()
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<Jobs>().FindAll().ToList();
            }
        }

        public void AddUpdateHost(Hosts host)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                host.LastConnected = DateTime.Now;

                var dbHost = db.GetCollection<Hosts>().FindOne(a => a.Name == host.Name);

                if (dbHost == null)
                {
                    db.GetCollection<Hosts>().Insert(host);
                } else
                {
                    host.ID = dbHost.ID;

                    db.GetCollection<Hosts>().Update(host);
                }
            }
        }

        public void DeleteHost(Guid id)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                db.GetCollection<Hosts>().Delete(a => a.ID == id);
            }
        }

        public List<Hosts> GetHosts()
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<Hosts>().FindAll().ToList();
            }
        }

        public List<PendingSubmissions> GetPendingSubmissions()
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                return db.GetCollection<PendingSubmissions>().FindAll().ToList();
            }
        }

        public void AddOfflineSubmission(Jobs job)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                var pendingSubmission = new PendingSubmissions
                {
                    ID = job.ID,
                    JobJSON = JsonConvert.SerializeObject(job)
                };

                db.GetCollection<PendingSubmissions>().Insert(pendingSubmission);
            }
        }

        public void RemoveOfflineSubmission(Guid id)
        {
            using (var db = new LiteDatabase(DbFilename))
            {
                db.GetCollection<PendingSubmissions>().Delete(a => a.ID == id);
            }
        }
    }
}