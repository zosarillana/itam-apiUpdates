﻿// <auto-generated />
using System;
using ITAM.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ITAM.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ITAM.Models.Approval.AccountabilityApproval", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<int?>("accountability_id")
                        .HasColumnType("int");

                    b.Property<string>("approved_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("approved_date")
                        .HasColumnType("date");

                    b.Property<string>("confirmed_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("confirmed_date")
                        .HasColumnType("date");

                    b.Property<string>("prepared_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("prepared_date")
                        .HasColumnType("date");

                    b.HasKey("id");

                    b.HasIndex("accountability_id");

                    b.ToTable("accountability_approval");
                });

            modelBuilder.Entity("ITAM.Models.Approval.ReturnItemApproval", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<int>("accountability_id")
                        .HasColumnType("int");

                    b.Property<string>("checked_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("checked_date")
                        .HasColumnType("date");

                    b.Property<string>("confirmed_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("confirmed_date")
                        .HasColumnType("date");

                    b.Property<string>("received_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("received_date")
                        .HasColumnType("date");

                    b.HasKey("id");

                    b.HasIndex("accountability_id");

                    b.ToTable("return_item_approval");
                });

            modelBuilder.Entity("ITAM.Models.Asset", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<int?>("UserAccountabilityListid")
                        .HasColumnType("int");

                    b.Property<string>("asset_barcode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("asset_image")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("brand")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("color")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("computer_id")
                        .HasColumnType("int");

                    b.Property<decimal>("cost")
                        .HasColumnType("decimal(18,4)");

                    b.Property<string>("date_acquired")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("date_created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("date_modified")
                        .HasColumnType("datetime2");

                    b.PrimitiveCollection<string>("history")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("is_deleted")
                        .HasColumnType("bit");

                    b.Property<string>("li_description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("model")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("owner_id")
                        .HasColumnType("int");

                    b.Property<string>("po")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("remarks")
                        .HasColumnType("nvarchar(max)");

                    b.PrimitiveCollection<string>("root_history")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("serial_no")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("size")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("warranty")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.HasIndex("UserAccountabilityListid");

                    b.HasIndex("computer_id");

                    b.HasIndex("owner_id");

                    b.ToTable("Assets");
                });

            modelBuilder.Entity("ITAM.Models.BusinessUnit", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("code")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("description")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("business_unit", (string)null);
                });

            modelBuilder.Entity("ITAM.Models.Computer", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<int?>("UserAccountabilityListid")
                        .HasColumnType("int");

                    b.Property<string>("asset_barcode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("asset_image")
                        .HasColumnType("nvarchar(max)");

                    b.PrimitiveCollection<string>("assigned_assets")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("board")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("brand")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("color")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("cost")
                        .HasColumnType("decimal(18,4)");

                    b.Property<string>("date_acquired")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("date_created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("date_modified")
                        .HasColumnType("datetime2");

                    b.Property<string>("gpu")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("hdd")
                        .HasColumnType("nvarchar(max)");

                    b.PrimitiveCollection<string>("history")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("is_deleted")
                        .HasColumnType("bit");

                    b.Property<string>("li_description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("model")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("owner_id")
                        .HasColumnType("int");

                    b.Property<string>("po")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ram")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("remarks")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("serial_no")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("size")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ssd")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("warranty")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.HasIndex("UserAccountabilityListid");

                    b.HasIndex("owner_id");

                    b.ToTable("computers");
                });

            modelBuilder.Entity("ITAM.Models.ComputerComponents", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("asset_barcode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("component_image")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("computer_id")
                        .HasColumnType("int");

                    b.Property<decimal>("cost")
                        .HasColumnType("decimal(18,4)");

                    b.Property<string>("date_acquired")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.PrimitiveCollection<string>("history")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool?>("is_deleted")
                        .HasColumnType("bit");

                    b.Property<int?>("owner_id")
                        .HasColumnType("int");

                    b.Property<string>("status")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("uid")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.HasIndex("computer_id");

                    b.HasIndex("owner_id");

                    b.ToTable("computer_components");
                });

            modelBuilder.Entity("ITAM.Models.Department", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("code")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("description")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("department", (string)null);
                });

            modelBuilder.Entity("ITAM.Models.Logs.Asset_logs", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("action")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("asset_id")
                        .HasColumnType("int");

                    b.Property<string>("details")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("performed_by_user_id")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("id");

                    b.HasIndex("asset_id");

                    b.ToTable("asset_Logs");
                });

            modelBuilder.Entity("ITAM.Models.Logs.CentralizedLogs", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("action")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("asset_barcode")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("details")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("performed_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("timestamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("type")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("centralized_logs");
                });

            modelBuilder.Entity("ITAM.Models.Logs.Computer_components_logs", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("action")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("computer_components_id")
                        .HasColumnType("int");

                    b.Property<string>("details")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("performed_by_user_id")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("id");

                    b.HasIndex("computer_components_id");

                    b.ToTable("computer_components_logs");
                });

            modelBuilder.Entity("ITAM.Models.Logs.Computer_logs", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("action")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("computer_id")
                        .HasColumnType("int");

                    b.Property<string>("details")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("performed_by_user_id")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("id");

                    b.HasIndex("computer_id");

                    b.ToTable("computer_Logs");
                });

            modelBuilder.Entity("ITAM.Models.Logs.Repair_logs", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("action")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("computer_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("eaf_no")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("inventory_code")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("item_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("performed_by_user_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("remarks")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("timestamp")
                        .HasColumnType("datetime2");

                    b.Property<string>("type")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("repair_logs");
                });

            modelBuilder.Entity("ITAM.Models.Logs.User_logs", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("action")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("details")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("performed_by_user_id")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("timestamp")
                        .HasColumnType("datetime2");

                    b.Property<int>("user_id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("user_id");

                    b.ToTable("user_logs");
                });

            modelBuilder.Entity("ITAM.Models.ReturnItems", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<int>("accountability_id")
                        .HasColumnType("int");

                    b.Property<int?>("asset_id")
                        .HasColumnType("int");

                    b.Property<int?>("component_id")
                        .HasColumnType("int");

                    b.Property<int?>("computer_id")
                        .HasColumnType("int");

                    b.Property<string>("item_type")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("remarks")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("return_date")
                        .HasColumnType("datetime2");

                    b.Property<string>("status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("user_id")
                        .HasColumnType("int");

                    b.Property<int>("validated_by")
                        .HasColumnType("int");

                    b.HasKey("id");

                    b.HasIndex("accountability_id");

                    b.HasIndex("asset_id");

                    b.HasIndex("component_id");

                    b.HasIndex("computer_id");

                    b.HasIndex("user_id");

                    b.ToTable("return_items");
                });

            modelBuilder.Entity("ITAM.Models.User", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("company")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("date_created")
                        .HasColumnType("datetime2");

                    b.Property<string>("date_hired")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("date_resignation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("department")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("designation")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("e_signature")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("employee_id")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("is_active")
                        .HasColumnType("bit");

                    b.Property<string>("name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("password")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("role")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ITAM.Models.UserAccountabilityList", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("id"));

                    b.Property<string>("accountability_code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("asset_ids")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("computer_ids")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("date_created")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("date_modified")
                        .HasColumnType("datetime2");

                    b.Property<bool>("is_active")
                        .HasColumnType("bit");

                    b.Property<bool>("is_deleted")
                        .HasColumnType("bit");

                    b.Property<int>("owner_id")
                        .HasColumnType("int");

                    b.Property<string>("tracking_code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.HasIndex("owner_id");

                    b.ToTable("user_accountability_lists");
                });

            modelBuilder.Entity("ITAM.Models.Approval.AccountabilityApproval", b =>
                {
                    b.HasOne("ITAM.Models.UserAccountabilityList", "accountability_list")
                        .WithMany()
                        .HasForeignKey("accountability_id")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("accountability_list");
                });

            modelBuilder.Entity("ITAM.Models.Approval.ReturnItemApproval", b =>
                {
                    b.HasOne("ITAM.Models.UserAccountabilityList", "accountability_list")
                        .WithMany()
                        .HasForeignKey("accountability_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("accountability_list");
                });

            modelBuilder.Entity("ITAM.Models.Asset", b =>
                {
                    b.HasOne("ITAM.Models.UserAccountabilityList", null)
                        .WithMany("assets")
                        .HasForeignKey("UserAccountabilityListid");

                    b.HasOne("ITAM.Models.Computer", "computer")
                        .WithMany("Assets")
                        .HasForeignKey("computer_id");

                    b.HasOne("ITAM.Models.User", "owner")
                        .WithMany("assets")
                        .HasForeignKey("owner_id")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("computer");

                    b.Navigation("owner");
                });

            modelBuilder.Entity("ITAM.Models.Computer", b =>
                {
                    b.HasOne("ITAM.Models.UserAccountabilityList", null)
                        .WithMany("computer")
                        .HasForeignKey("UserAccountabilityListid");

                    b.HasOne("ITAM.Models.User", "owner")
                        .WithMany("computer")
                        .HasForeignKey("owner_id")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("owner");
                });

            modelBuilder.Entity("ITAM.Models.ComputerComponents", b =>
                {
                    b.HasOne("ITAM.Models.Computer", "computer")
                        .WithMany("Components")
                        .HasForeignKey("computer_id")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("ITAM.Models.User", "owner")
                        .WithMany("computer_components")
                        .HasForeignKey("owner_id")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("computer");

                    b.Navigation("owner");
                });

            modelBuilder.Entity("ITAM.Models.Logs.Asset_logs", b =>
                {
                    b.HasOne("ITAM.Models.Asset", "assets")
                        .WithMany()
                        .HasForeignKey("asset_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("assets");
                });

            modelBuilder.Entity("ITAM.Models.Logs.Computer_components_logs", b =>
                {
                    b.HasOne("ITAM.Models.ComputerComponents", "computer_components")
                        .WithMany()
                        .HasForeignKey("computer_components_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("computer_components");
                });

            modelBuilder.Entity("ITAM.Models.Logs.Computer_logs", b =>
                {
                    b.HasOne("ITAM.Models.Computer", "computer")
                        .WithMany()
                        .HasForeignKey("computer_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("computer");
                });

            modelBuilder.Entity("ITAM.Models.Logs.User_logs", b =>
                {
                    b.HasOne("ITAM.Models.User", "user")
                        .WithMany()
                        .HasForeignKey("user_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("user");
                });

            modelBuilder.Entity("ITAM.Models.ReturnItems", b =>
                {
                    b.HasOne("ITAM.Models.UserAccountabilityList", "user_accountability_list")
                        .WithMany("ReturnItems")
                        .HasForeignKey("accountability_id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ITAM.Models.Asset", "asset")
                        .WithMany("ReturnItems")
                        .HasForeignKey("asset_id")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ITAM.Models.ComputerComponents", "components")
                        .WithMany("ReturnItems")
                        .HasForeignKey("component_id")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ITAM.Models.Computer", "computer")
                        .WithMany("ReturnItems")
                        .HasForeignKey("computer_id")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ITAM.Models.User", "user")
                        .WithMany("ReturnItems")
                        .HasForeignKey("user_id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("asset");

                    b.Navigation("components");

                    b.Navigation("computer");

                    b.Navigation("user");

                    b.Navigation("user_accountability_list");
                });

            modelBuilder.Entity("ITAM.Models.UserAccountabilityList", b =>
                {
                    b.HasOne("ITAM.Models.User", "owner")
                        .WithMany()
                        .HasForeignKey("owner_id")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("owner");
                });

            modelBuilder.Entity("ITAM.Models.Asset", b =>
                {
                    b.Navigation("ReturnItems");
                });

            modelBuilder.Entity("ITAM.Models.Computer", b =>
                {
                    b.Navigation("Assets");

                    b.Navigation("Components");

                    b.Navigation("ReturnItems");
                });

            modelBuilder.Entity("ITAM.Models.ComputerComponents", b =>
                {
                    b.Navigation("ReturnItems");
                });

            modelBuilder.Entity("ITAM.Models.User", b =>
                {
                    b.Navigation("ReturnItems");

                    b.Navigation("assets");

                    b.Navigation("computer");

                    b.Navigation("computer_components");
                });

            modelBuilder.Entity("ITAM.Models.UserAccountabilityList", b =>
                {
                    b.Navigation("ReturnItems");

                    b.Navigation("assets");

                    b.Navigation("computer");
                });
#pragma warning restore 612, 618
        }
    }
}
