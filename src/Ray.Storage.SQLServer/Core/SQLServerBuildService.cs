﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Ray.Storage.SQLCore;
using Ray.Storage.SQLCore.Configuration;
using Ray.Storage.SQLCore.Services;

namespace Ray.Storage.SQLServer
{
    public class SQLServerBuildService : IBuildService
    {
        private readonly StringKeyOptions stringStorageOptions;
        private readonly StorageOptions storageOptions;
        private readonly bool stateIdIsString;

        public SQLServerBuildService(StorageOptions storageOptions)
        {
            this.storageOptions = storageOptions;
            if (storageOptions is StringKeyOptions options)
            {
                this.stateIdIsString = true;
                this.stringStorageOptions = options;
            }
            else
            {
                this.stateIdIsString = false;
            }
        }

        public async Task<List<EventSubTable>> GetSubTables()
        {
            string sql = "SELECT * FROM SubTable_Records where TableName=@TableName";
            using var connection = this.storageOptions.CreateConnection();
            return (await connection.QueryAsync<EventSubTable>(sql, new { TableName = this.storageOptions.EventTable })).AsList();
        }

        public async Task<bool> CreateEventSubTable()
        {
            const string sql = @"
                    if not exists(select 1 from sysobjects where id = object_id('SubTable_Records')and type = 'U')
                        CREATE TABLE SubTable_Records
                        (
	                        TableName varchar(255) NOT NULL,
	                        SubTable varchar(255) NOT NULL,
	                        [Index] int NOT NULL,
	                        StartTime bigint NOT NULL,
	                        EndTime bigint NOT NULL,
                            INDEX subtable_record UNIQUE(TableName, [Index])
                        )";
            using var connection = this.storageOptions.CreateConnection();
            return await connection.ExecuteAsync(sql) > 0;
        }

        public async Task CreateEventTable(EventSubTable subTable)
        {
            var stateIdSql = this.stateIdIsString ? $"StateId varchar({this.stringStorageOptions.StateIdLength}) not null" : "StateId bigint not null";
            var sql = $@"
                    create table {subTable.SubTable} (
                            {stateIdSql},
                            UniqueId varchar(250)  null,
                            TypeCode varchar(300)  not null,
                            Data nvarchar(max) not null,
                            Version bigint not null,
                            Timestamp bigint not null,
                            INDEX {subTable.SubTable}_id_unique unique(StateId,TypeCode,UniqueId),
							INDEX {subTable.SubTable}_Version unique(StateId, Version)
                            );";
            const string insertSql = "INSERT into SubTable_Records  VALUES(@TableName,@SubTable,@Index,@StartTime,@EndTime)";
            using var connection = this.storageOptions.CreateConnection();
            await connection.OpenAsync();
            using var trans = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(sql, transaction: trans);
                await connection.ExecuteAsync(insertSql, subTable, trans);
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public async Task CreateEventArchiveTable()
        {
            var stateIdSql = this.stateIdIsString ? $"StateId varchar({this.stringStorageOptions.StateIdLength}) not null" : "StateId bigint not null";
            var sql = $@"
                    if not exists(select 1 from sysobjects where id = object_id('{this.storageOptions.EventArchiveTable}')and type = 'U')
                    create table {this.storageOptions.EventArchiveTable} (
                            {stateIdSql},
                            UniqueId varchar(250)  null,
                            TypeCode varchar(300)  not null,
                            Data nvarchar(max) not null,
                            Version bigint not null,
                            Timestamp bigint not null,
                            INDEX {this.storageOptions.EventArchiveTable}_id_unique unique(StateId,TypeCode,UniqueId),
                            INDEX {this.storageOptions.EventArchiveTable}_Version unique(StateId, Version)
                            );";
            using var connection = this.storageOptions.CreateConnection();
            await connection.ExecuteAsync(sql);
        }

        public async Task CreateObserverSnapshotTable(string observerSnapshotTable)
        {
            var stateIdSql = this.stateIdIsString ? $"StateId varchar({this.stringStorageOptions.StateIdLength}) not null PRIMARY KEY" : "StateId bigint not null PRIMARY KEY";
            var sql = $@"
                    if not exists(select 1 from sysobjects where id = object_id('{observerSnapshotTable}')and type = 'U')
                     CREATE TABLE {observerSnapshotTable}(
                     {stateIdSql},
                     StartTimestamp bigint not null,
                     Version bigint not null);";
            using var connection = this.storageOptions.CreateConnection();
            await connection.ExecuteAsync(sql);
        }

        public async Task CreateSnapshotArchiveTable()
        {
            var stateIdSql = this.stateIdIsString ? $"StateId varchar({this.stringStorageOptions.StateIdLength}) not null" : "StateId bigint not null";
            var sql = $@"
                     if not exists(select 1 from sysobjects where id = object_id('{this.storageOptions.SnapshotArchiveTable}')and type = 'U')
                     CREATE TABLE {this.storageOptions.SnapshotArchiveTable}(
                     Id varchar(50) not null PRIMARY KEY,
                     {stateIdSql},
                     StartVersion bigint not null,
                     EndVersion bigint not null,
                     StartTimestamp bigint not null,
                     EndTimestamp bigint not null,
                     [Index] int not null,
                     EventIsCleared bit not null,
                     Data nvarchar(max) not null,
                     IsOver bit not null,
                     Version bigint not null,
                     INDEX {this.storageOptions.SnapshotArchiveTable}_StateId NONCLUSTERED(StateId)
                      );";
            using var connection = this.storageOptions.CreateConnection();
            await connection.ExecuteAsync(sql);
        }

        public async Task CreateSnapshotTable()
        {
            var stateIdSql = this.stateIdIsString ? $"StateId varchar({this.stringStorageOptions.StateIdLength}) not null PRIMARY KEY" : "StateId bigint not null PRIMARY KEY";
            var sql = $@"
                     if not exists(select 1 from sysobjects where id = object_id('{this.storageOptions.SnapshotTable}')and type = 'U')
                     CREATE TABLE {this.storageOptions.SnapshotTable}(
                     {stateIdSql},
                     Data nvarchar(max) not null,
                     Version bigint not null,
                     StartTimestamp bigint not null,
                     LatestMinEventTimestamp bigint not null,
                     IsLatest bit not null,
                     IsOver bit not null);";
            using var connection = this.storageOptions.CreateConnection();
            await connection.ExecuteAsync(sql);
        }
    }
}
