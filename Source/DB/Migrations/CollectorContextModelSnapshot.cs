﻿// <auto-generated />
using System;
using GoodsTracker.DataCollector.DB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GoodsTracker.DataCollector.DB.Migrations
{
    [DbContext(typeof(CollectorContext))]
    partial class CollectorContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "btree_gin");
            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "pg_trgm");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CategoryItem", b =>
                {
                    b.Property<int>("CategoriesId")
                        .HasColumnType("integer")
                        .HasColumnName("categories_id");

                    b.Property<int>("ItemsId")
                        .HasColumnType("integer")
                        .HasColumnName("items_id");

                    b.HasKey("CategoriesId", "ItemsId")
                        .HasName("pk_category_item");

                    b.HasIndex("ItemsId")
                        .HasDatabaseName("ix_category_item_items_id");

                    b.ToTable("category_item", (string)null);
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.Category", b =>
                {
                    b.Property<int>("Id")
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("Id")
                        .HasName("pk_categories");

                    b.HasIndex("Name")
                        .HasDatabaseName("ix_categories_name");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Name"), "gin");
                    NpgsqlIndexBuilderExtensions.HasOperators(b.HasIndex("Name"), new[] { "gin_trgm_ops" });

                    b.ToTable("categories", (string)null);
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.FavoriteItem", b =>
                {
                    b.Property<int>("ItemId")
                        .HasColumnType("integer")
                        .HasColumnName("item_id");

                    b.Property<string>("UserId")
                        .HasColumnType("text")
                        .HasColumnName("user_id");

                    b.Property<DateTime>("DateAdded")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date_added");

                    b.HasKey("ItemId", "UserId")
                        .HasName("pk_favorite_items");

                    b.ToTable("favorite_items", (string)null);
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.Item", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("Adult")
                        .HasColumnType("boolean")
                        .HasColumnName("adult");

                    b.Property<float?>("Carbo")
                        .HasColumnType("real")
                        .HasColumnName("carbo");

                    b.Property<string>("Compound")
                        .HasColumnType("text")
                        .HasColumnName("compound");

                    b.Property<string>("Country")
                        .HasColumnType("text")
                        .HasColumnName("country");

                    b.Property<float?>("Fat")
                        .HasColumnType("real")
                        .HasColumnName("fat");

                    b.Property<string>("ImageLink")
                        .HasColumnType("text")
                        .HasColumnName("image_link");

                    b.Property<string>("Metadata")
                        .HasColumnType("text")
                        .HasColumnName("metadata");

                    b.Property<string>("Name1")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name1");

                    b.Property<string>("Name2")
                        .HasColumnType("text")
                        .HasColumnName("name2");

                    b.Property<string>("Name3")
                        .HasColumnType("text")
                        .HasColumnName("name3");

                    b.Property<float?>("Portion")
                        .HasColumnType("real")
                        .HasColumnName("portion");

                    b.Property<string>("Producer")
                        .HasColumnType("text")
                        .HasColumnName("producer");

                    b.Property<float?>("Protein")
                        .HasColumnType("real")
                        .HasColumnName("protein");

                    b.Property<Guid?>("PublicId")
                        .HasColumnType("uuid")
                        .HasColumnName("public_id");

                    b.Property<long?>("VendorCode")
                        .HasColumnType("bigint")
                        .HasColumnName("vendor_code");

                    b.Property<int>("VendorId")
                        .HasColumnType("integer")
                        .HasColumnName("vendor_id");

                    b.Property<double?>("Weight")
                        .HasColumnType("double precision")
                        .HasColumnName("weight");

                    b.Property<string>("WeightUnit")
                        .HasColumnType("text")
                        .HasColumnName("weight_unit");

                    b.HasKey("Id")
                        .HasName("pk_items");

                    b.HasIndex("Name1")
                        .HasDatabaseName("ix_items_name1");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("Name1"), "gin");
                    NpgsqlIndexBuilderExtensions.HasOperators(b.HasIndex("Name1"), new[] { "gin_trgm_ops" });

                    b.HasIndex("VendorId")
                        .HasDatabaseName("ix_items_vendor_id");

                    b.ToTable("items", (string)null);
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.ItemRecord", b =>
                {
                    b.Property<int>("ItemId")
                        .HasColumnType("integer")
                        .HasColumnName("item_id");

                    b.Property<int>("StreamId")
                        .HasColumnType("integer")
                        .HasColumnName("stream_id");

                    b.Property<decimal?>("CutPrice")
                        .HasPrecision(9, 2)
                        .HasColumnType("numeric(9,2)")
                        .HasColumnName("cut_price");

                    b.Property<bool>("OnDiscount")
                        .HasColumnType("boolean")
                        .HasColumnName("on_discount");

                    b.Property<decimal>("Price")
                        .HasPrecision(9, 2)
                        .HasColumnType("numeric(9,2)")
                        .HasColumnName("price");

                    b.HasKey("ItemId", "StreamId")
                        .HasName("pk_item_records");

                    b.HasIndex("StreamId")
                        .HasDatabaseName("ix_item_records_stream_id");

                    b.ToTable("item_records", (string)null);
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.Stream", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("FetchDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("fetch_date");

                    b.HasKey("Id")
                        .HasName("pk_streams");

                    b.ToTable("streams", (string)null);
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.Vendor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Land")
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)")
                        .HasColumnName("land");

                    b.Property<string>("LogoImageLink")
                        .HasColumnType("text")
                        .HasColumnName("logo_image_link");

                    b.Property<string>("Name1")
                        .HasColumnType("text")
                        .HasColumnName("name1");

                    b.Property<string>("Name2")
                        .HasColumnType("text")
                        .HasColumnName("name2");

                    b.Property<string>("Name3")
                        .HasColumnType("text")
                        .HasColumnName("name3");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.Property<string>("Website")
                        .HasColumnType("text")
                        .HasColumnName("website");

                    b.HasKey("Id")
                        .HasName("pk_vendors");

                    b.ToTable("vendors", (string)null);

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Land = "BYN",
                            LogoImageLink = "https://sosedi.by/upload/medialibrary/418/4181b7febf77b139d9936faf5b5095ce.png",
                            Name1 = "Соседи",
                            Name2 = "Sosedi",
                            Name3 = "Sosedi Inc.",
                            Type = 0,
                            Website = "https://sosedi.by/"
                        },
                        new
                        {
                            Id = 3,
                            Land = "BYN",
                            LogoImageLink = "https://api.static.edostavka.by/media/63c953ca1fae9_ed-logo.svg?id=12369",
                            Name1 = "Евроопт",
                            Name2 = "Evroopt",
                            Name3 = "Edostavka",
                            Type = 0,
                            Website = "https://edostavka.by/"
                        },
                        new
                        {
                            Id = 4,
                            Land = "BYN",
                            LogoImageLink = "https://green-dostavka.by/images/logo.svg",
                            Name1 = "Green",
                            Name2 = "Green",
                            Name3 = "Green",
                            Type = 0,
                            Website = "https://green-dostavka.by/"
                        },
                        new
                        {
                            Id = 5,
                            Land = "BYN",
                            LogoImageLink = "https://bel-market.by/bitrix/templates/info_light_blue/img/svg/logo.svg",
                            Name1 = "Белмаркет",
                            Name2 = "Belmarket",
                            Name3 = "BELMARKET",
                            Type = 0,
                            Website = "https://bel-market.by/"
                        },
                        new
                        {
                            Id = 6,
                            Land = "BYN",
                            LogoImageLink = "https://gippo-market.by/local/templates/html/images/i-gippo.svg",
                            Name1 = "Гиппо",
                            Name2 = "Gippo-market",
                            Name3 = "Gippo",
                            Type = 0,
                            Website = "https://gippo-market.by/"
                        });
                });

            modelBuilder.Entity("CategoryItem", b =>
                {
                    b.HasOne("GoodsTracker.DataCollector.DB.Entities.Category", null)
                        .WithMany()
                        .HasForeignKey("CategoriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_category_item_categories_categories_id");

                    b.HasOne("GoodsTracker.DataCollector.DB.Entities.Item", null)
                        .WithMany()
                        .HasForeignKey("ItemsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_category_item_items_items_id");
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.FavoriteItem", b =>
                {
                    b.HasOne("GoodsTracker.DataCollector.DB.Entities.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_favorite_items_items_item_id");

                    b.Navigation("Item");
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.Item", b =>
                {
                    b.HasOne("GoodsTracker.DataCollector.DB.Entities.Vendor", "Vendor")
                        .WithMany()
                        .HasForeignKey("VendorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_items_vendors_vendor_id");

                    b.Navigation("Vendor");
                });

            modelBuilder.Entity("GoodsTracker.DataCollector.DB.Entities.ItemRecord", b =>
                {
                    b.HasOne("GoodsTracker.DataCollector.DB.Entities.Item", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_item_records_items_item_id");

                    b.HasOne("GoodsTracker.DataCollector.DB.Entities.Stream", "Stream")
                        .WithMany()
                        .HasForeignKey("StreamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_item_records_streams_stream_id");

                    b.Navigation("Item");

                    b.Navigation("Stream");
                });
#pragma warning restore 612, 618
        }
    }
}
