﻿// <auto-generated />
using System;
using DuaBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DuaBot.Migrations
{
    [DbContext(typeof(DuaBotContext))]
    [Migration("20190305070425_First")]
    partial class First
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034");

            modelBuilder.Entity("DuaBot.Data.SlackUpdateTask", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset>("End");

                    b.Property<string>("SlackUserId");

                    b.Property<DateTimeOffset>("Start");

                    b.Property<string>("Subject");

                    b.Property<string>("TimeZone");

                    b.HasKey("Id");

                    b.ToTable("SlackUpdateTasks");
                });

            modelBuilder.Entity("DuaBot.Data.UserTokenMap", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AccessToken");

                    b.Property<DateTime>("DateAdded");

                    b.Property<string>("RefreshToken");

                    b.Property<string>("SlackId");

                    b.HasKey("Id");

                    b.ToTable("UserTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
