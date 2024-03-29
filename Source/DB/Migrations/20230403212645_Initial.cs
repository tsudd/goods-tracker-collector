﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GoodsTracker.DataCollector.DB.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gin", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "producers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "streams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fetch_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_streams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name1 = table.Column<string>(type: "text", nullable: true),
                    name2 = table.Column<string>(type: "text", nullable: true),
                    name3 = table.Column<string>(type: "text", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    website = table.Column<string>(type: "text", nullable: true),
                    land = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    logo_image_link = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_errors",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    error_type = table.Column<int>(type: "integer", nullable: false),
                    details = table.Column<string>(type: "text", nullable: false),
                    serialied_item = table.Column<string>(type: "text", nullable: true),
                    stream_id = table.Column<int>(type: "integer", nullable: false),
                    resolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_errors", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_errors_streams_stream_id",
                        column: x => x.stream_id,
                        principalTable: "streams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name1 = table.Column<string>(type: "text", nullable: false),
                    name2 = table.Column<string>(type: "text", nullable: true),
                    name3 = table.Column<string>(type: "text", nullable: true),
                    image_link = table.Column<string>(type: "text", nullable: true),
                    weight = table.Column<float>(type: "real", nullable: true),
                    weight_unit = table.Column<string>(type: "text", nullable: true),
                    vendor_code = table.Column<long>(type: "bigint", nullable: true),
                    compound = table.Column<string>(type: "text", nullable: true),
                    protein = table.Column<float>(type: "real", nullable: true),
                    fat = table.Column<float>(type: "real", nullable: true),
                    carbo = table.Column<float>(type: "real", nullable: true),
                    portion = table.Column<float>(type: "real", nullable: true),
                    producer_id = table.Column<long>(type: "bigint", nullable: true),
                    adult = table.Column<bool>(type: "boolean", nullable: false),
                    vendor_id = table.Column<int>(type: "integer", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_items_producers_producer_id",
                        column: x => x.producer_id,
                        principalTable: "producers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_items_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "category_item",
                columns: table => new
                {
                    categories_id = table.Column<long>(type: "bigint", nullable: false),
                    items_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_item", x => new { x.categories_id, x.items_id });
                    table.ForeignKey(
                        name: "fk_category_item_categories_categories_id",
                        column: x => x.categories_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_category_item_items_items_id",
                        column: x => x.items_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favorite_items",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    date_added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_favorite_items", x => new { x.item_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_favorite_items_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_records",
                columns: table => new
                {
                    item_id = table.Column<int>(type: "integer", nullable: false),
                    stream_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    cut_price = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: true),
                    on_discount = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_records", x => new { x.item_id, x.stream_id });
                    table.ForeignKey(
                        name: "fk_item_records_items_item_id",
                        column: x => x.item_id,
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_item_records_streams_stream_id",
                        column: x => x.stream_id,
                        principalTable: "streams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "vendors",
                columns: new[] { "id", "land", "logo_image_link", "name1", "name2", "name3", "type", "website" },
                values: new object[,]
                {
                    { 1, "BYN", "https://sosedi.by/upload/medialibrary/418/4181b7febf77b139d9936faf5b5095ce.png", "Соседи", "Sosedi", "Sosedi Inc.", 0, "https://sosedi.by/" },
                    { 3, "BYN", "https://api.static.edostavka.by/media/63c953ca1fae9_ed-logo.svg?id=12369", "Евроопт", "Evroopt", "Edostavka", 0, "https://edostavka.by/" },
                    { 4, "BYN", "https://green-dostavka.by/images/logo.svg", "Green", "Green", "Green", 0, "https://green-dostavka.by/" },
                    { 5, "BYN", "https://bel-market.by/bitrix/templates/info_light_blue/img/svg/logo.svg", "Белмаркет", "Belmarket", "BELMARKET", 0, "https://bel-market.by/" },
                    { 6, "BYN", "https://gippo-market.by/local/templates/html/images/i-gippo.svg", "Гиппо", "Gippo-market", "Gippo", 0, "https://gippo-market.by/" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_category_item_items_id",
                table: "category_item",
                column: "items_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_errors_stream_id",
                table: "item_errors",
                column: "stream_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_records_stream_id",
                table: "item_records",
                column: "stream_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_name1",
                table: "items",
                column: "name1")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_items_producer_id",
                table: "items",
                column: "producer_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_vendor_id",
                table: "items",
                column: "vendor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_item");

            migrationBuilder.DropTable(
                name: "favorite_items");

            migrationBuilder.DropTable(
                name: "item_errors");

            migrationBuilder.DropTable(
                name: "item_records");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "streams");

            migrationBuilder.DropTable(
                name: "producers");

            migrationBuilder.DropTable(
                name: "vendors");
        }
    }
}
