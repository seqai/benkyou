﻿// <auto-generated />
using System;
using Benkyou.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Benkyou.Migrations
{
    [DbContext(typeof(BenkyouDbContext))]
    [Migration("20230109195832_AddTags")]
    partial class AddTags
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Benkyou.DAL.Entities.Record", b =>
                {
                    b.Property<int>("RecordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("RecordId"));

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("Ignored")
                        .HasColumnType("boolean");

                    b.Property<int>("RecordType")
                        .HasColumnType("integer");

                    b.Property<int>("Score")
                        .HasColumnType("integer");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("RecordId");

                    b.HasIndex("UserId", "Content", "RecordType")
                        .IsUnique();

                    b.ToTable("Records");
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.RecordHit", b =>
                {
                    b.Property<int>("RecordHitId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("RecordHitId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("HitScore")
                        .HasColumnType("integer");

                    b.Property<bool>("Ignored")
                        .HasColumnType("boolean");

                    b.Property<int>("RecordId")
                        .HasColumnType("integer");

                    b.HasKey("RecordHitId");

                    b.HasIndex("RecordId");

                    b.ToTable("RecordHits");
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.Tag", b =>
                {
                    b.Property<int>("TagId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("TagId"));

                    b.Property<int?>("AutoTagId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("TagId");

                    b.HasIndex("UserId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("UserId"));

                    b.Property<string>("AutoTag")
                        .HasColumnType("text");

                    b.Property<DateTime>("AutoTagValidFrom")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("AutoTagValidityMinutes")
                        .HasColumnType("integer");

                    b.Property<int>("DefaultRecordType")
                        .HasColumnType("integer");

                    b.Property<long>("TelegramId")
                        .HasColumnType("bigint");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("UserId");

                    b.HasIndex("TelegramId")
                        .IsUnique();

                    b.HasIndex("Username")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("RecordTag", b =>
                {
                    b.Property<int>("RecordsRecordId")
                        .HasColumnType("integer");

                    b.Property<int>("TagsTagId")
                        .HasColumnType("integer");

                    b.HasKey("RecordsRecordId", "TagsTagId");

                    b.HasIndex("TagsTagId");

                    b.ToTable("RecordTags", (string)null);
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.Record", b =>
                {
                    b.HasOne("Benkyou.DAL.Entities.User", "User")
                        .WithMany("Records")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.RecordHit", b =>
                {
                    b.HasOne("Benkyou.DAL.Entities.Record", "Record")
                        .WithMany("Hits")
                        .HasForeignKey("RecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Record");
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.Tag", b =>
                {
                    b.HasOne("Benkyou.DAL.Entities.User", "User")
                        .WithMany("Tags")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("RecordTag", b =>
                {
                    b.HasOne("Benkyou.DAL.Entities.Record", null)
                        .WithMany()
                        .HasForeignKey("RecordsRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Benkyou.DAL.Entities.Tag", null)
                        .WithMany()
                        .HasForeignKey("TagsTagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.Record", b =>
                {
                    b.Navigation("Hits");
                });

            modelBuilder.Entity("Benkyou.DAL.Entities.User", b =>
                {
                    b.Navigation("Records");

                    b.Navigation("Tags");
                });
#pragma warning restore 612, 618
        }
    }
}
