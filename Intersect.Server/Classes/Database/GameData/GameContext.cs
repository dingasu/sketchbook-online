﻿using System;
using System.Data;
using Intersect.GameObjects;
using Intersect.GameObjects.Crafting;
using Intersect.GameObjects.Events;
using Intersect.GameObjects.Maps.MapList;
using Intersect.Server.Classes.Database.GameData.Migrations;
using Intersect.Server.Maps;
using Intersect.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Intersect.Server.Database.GameData
{
    public class GameContext : DbContext
    {
        public static GameContext Current { get; private set; }

        //Animations
        public DbSet<AnimationBase> Animations { get; set; }

        //Crafting
        public DbSet<CraftBase> Crafts { get; set; }
        public DbSet<CraftingTableBase> CraftingTables { get; set; }

        //Classes
        public DbSet<ClassBase> Classes { get; set; }

        //Events
        public DbSet<EventBase> Events { get; set; }

        //Items
        public DbSet<ItemBase> Items { get; set; }

        //Maps
        public DbSet<MapInstance> Maps { get; set; }
        public DbSet<MapList> MapFolders { get; set; }

        //NPCs
        public DbSet<NpcBase> Npcs { get; set; }

        //Projectiles
        public DbSet<ProjectileBase> Projectiles { get; set; }

        //Quests
        public DbSet<QuestBase> Quests { get; set; }

        //Resources
        public DbSet<ResourceBase> Resources { get; set; }

        //Shops
        public DbSet<ShopBase> Shops { get; set; }

        //Spells
        public DbSet<SpellBase> Spells { get; set; }

        //Variables
        public DbSet<PlayerVariableBase> PlayerVariables { get; set; }
        public DbSet<ServerVariableBase> ServerVariables { get; set; }

        //Tilesets
        public DbSet<TilesetBase> Tilesets { get; set; }

        //Time
        public DbSet<TimeBase> Time { get; set; }

        private DatabaseUtils.DbProvider mConnection = DatabaseUtils.DbProvider.Sqlite;
        private string mConnectionString = @"Data Source=resources/gamedata.db";

        public GameContext()
        {
            Current = this;
        }

        public GameContext(DatabaseUtils.DbProvider connection, string connectionString)
        {
            mConnection = connection;
            mConnectionString = connectionString;
            Current = this;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging(true);
            switch (mConnection)
            {
                case DatabaseUtils.DbProvider.Sqlite:
                    optionsBuilder.UseSqlite(mConnectionString);
                    break;
                case DatabaseUtils.DbProvider.MySql:
                    optionsBuilder.UseMySql(mConnectionString);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsEmpty()
        {
            using (var command = Database.GetDbConnection().CreateCommand())
            {
                if (mConnection == DatabaseUtils.DbProvider.MySql)
                {
                    command.CommandText = "show tables;";
                }
                else if (mConnection == DatabaseUtils.DbProvider.Sqlite)
                {
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                }
                command.CommandType = CommandType.Text;

                Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    return !result.HasRows;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public void MigrationsProcessed(string[] migrations)
        {
            if (migrations.IndexOf("20190611170819_CombiningSwitchesVariables") > -1) Beta6Migration.Run(this);
        }
    }
}