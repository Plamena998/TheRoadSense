using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadSense.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bridges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoadRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Km = table.Column<double>(type: "float", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bridges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Selectable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "DataSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirstAidKitItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstAidKitItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirstAidSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Lead = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstAidSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FirstAidTopics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstAidTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomeStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HourWeekdayStats",
                columns: table => new
                {
                    Weekday = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    Ptp = table.Column<int>(type: "int", nullable: false),
                    Killed = table.Column<int>(type: "int", nullable: false),
                    Injured = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourWeekdayStats", x => new { x.Weekday, x.Hour });
                });

            migrationBuilder.CreateTable(
                name: "HourWeights",
                columns: table => new
                {
                    Hour = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourWeights", x => x.Hour);
                });

            migrationBuilder.CreateTable(
                name: "MonthWeights",
                columns: table => new
                {
                    Month = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthWeights", x => x.Month);
                });

            migrationBuilder.CreateTable(
                name: "Oblasts",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oblasts", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    NearRoad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RankingRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Road = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LengthKm = table.Column<int>(type: "int", nullable: false),
                    Crashes = table.Column<int>(type: "int", nullable: false),
                    Deaths = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingRows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roads",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LengthKm = table.Column<int>(type: "int", nullable: false),
                    CrashesPerYear = table.Column<int>(type: "int", nullable: false),
                    DeathsPerYear = table.Column<int>(type: "int", nullable: false),
                    InjuredPerYear = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpeedLimits",
                columns: table => new
                {
                    RoadType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Vehicle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LimitKmh = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeedLimits", x => new { x.RoadType, x.Vehicle });
                });

            migrationBuilder.CreateTable(
                name: "SpeedReductionRules",
                columns: table => new
                {
                    Condition = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReductionPercent = table.Column<int>(type: "int", nullable: false),
                    MaxSpeedKmh = table.Column<int>(type: "int", nullable: true),
                    Advice = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeedReductionRules", x => x.Condition);
                });

            migrationBuilder.CreateTable(
                name: "TickerItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Road = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tunnels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoadRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Km = table.Column<double>(type: "float", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tunnels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    AccuracyKm = table.Column<int>(type: "int", nullable: false),
                    NearRoad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemperatureMean = table.Column<double>(type: "float", nullable: false),
                    TemperatureAmplitude = table.Column<double>(type: "float", nullable: false),
                    WarmestHour = table.Column<int>(type: "int", nullable: false),
                    Sunrise = table.Column<int>(type: "int", nullable: false),
                    Sunset = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadSegments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    FromCityId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ToCityId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Road = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Km = table.Column<double>(type: "float", nullable: false),
                    FogExposure = table.Column<double>(type: "float", nullable: false),
                    History = table.Column<double>(type: "float", nullable: false),
                    Incident = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Repair = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadSegments_Cities_FromCityId",
                        column: x => x.FromCityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoadSegments_Cities_ToCityId",
                        column: x => x.ToCityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FirstAidStepItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstAidStepItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirstAidStepItems_FirstAidSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "FirstAidSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FirstAidTopicItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirstAidTopicItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FirstAidTopicItems_FirstAidTopics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "FirstAidTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OblastMonthlyPtp",
                columns: table => new
                {
                    Oblast = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Ptp = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OblastMonthlyPtp", x => new { x.Oblast, x.Year, x.Month });
                    table.ForeignKey(
                        name: "FK_OblastMonthlyPtp_Oblasts_Oblast",
                        column: x => x.Oblast,
                        principalTable: "Oblasts",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OblastPeriodStats",
                columns: table => new
                {
                    Oblast = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Ptp = table.Column<int>(type: "int", nullable: false),
                    Killed = table.Column<int>(type: "int", nullable: false),
                    Injured = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OblastPeriodStats", x => new { x.Oblast, x.Period });
                    table.ForeignKey(
                        name: "FK_OblastPeriodStats_Oblasts_Oblast",
                        column: x => x.Oblast,
                        principalTable: "Oblasts",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OblastRisks",
                columns: table => new
                {
                    Oblast = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ptp2026 = table.Column<int>(type: "int", nullable: false),
                    Killed2026 = table.Column<int>(type: "int", nullable: false),
                    Injured2026 = table.Column<int>(type: "int", nullable: false),
                    Ptp2025 = table.Column<int>(type: "int", nullable: false),
                    Killed2025 = table.Column<int>(type: "int", nullable: false),
                    Injured2025 = table.Column<int>(type: "int", nullable: false),
                    PtpChangePct = table.Column<double>(type: "float", nullable: false),
                    KilledPer100Ptp = table.Column<double>(type: "float", nullable: false),
                    InjuredPer100Ptp = table.Column<double>(type: "float", nullable: false),
                    SharePctOfCountry = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OblastRisks", x => x.Oblast);
                    table.ForeignKey(
                        name: "FK_OblastRisks_Oblasts_Oblast",
                        column: x => x.Oblast,
                        principalTable: "Oblasts",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherDaily",
                columns: table => new
                {
                    Oblast = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TempMin = table.Column<double>(type: "float", nullable: true),
                    TempMax = table.Column<double>(type: "float", nullable: true),
                    TempMean = table.Column<double>(type: "float", nullable: true),
                    PrecipitationMm = table.Column<double>(type: "float", nullable: true),
                    SnowfallCm = table.Column<double>(type: "float", nullable: true),
                    HumidityMean = table.Column<double>(type: "float", nullable: true),
                    CloudMean = table.Column<double>(type: "float", nullable: true),
                    WindMaxKmh = table.Column<double>(type: "float", nullable: true),
                    GustMaxKmh = table.Column<double>(type: "float", nullable: true),
                    WeatherCodeMax = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherDaily", x => new { x.Oblast, x.Date });
                    table.ForeignKey(
                        name: "FK_WeatherDaily_Oblasts_Oblast",
                        column: x => x.Oblast,
                        principalTable: "Oblasts",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlackSpots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoadId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Km = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlackSpots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlackSpots_Roads_RoadId",
                        column: x => x.RoadId,
                        principalTable: "Roads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Deaths = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoadId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Roads_RoadId",
                        column: x => x.RoadId,
                        principalTable: "Roads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RoadCauses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoadId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Percent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadCauses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadCauses_Roads_RoadId",
                        column: x => x.RoadId,
                        principalTable: "Roads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoadRegionWeights",
                columns: table => new
                {
                    RoadId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadRegionWeights", x => new { x.RoadId, x.Region });
                    table.ForeignKey(
                        name: "FK_RoadRegionWeights_Roads_RoadId",
                        column: x => x.RoadId,
                        principalTable: "Roads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeatherHourProfiles",
                columns: table => new
                {
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    FogIndex = table.Column<double>(type: "float", nullable: false),
                    PrecipitationMm = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherHourProfiles", x => new { x.LocationId, x.Hour });
                    table.ForeignKey(
                        name: "FK_WeatherHourProfiles_WeatherLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "WeatherLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlackSpots_RoadId",
                table: "BlackSpots",
                column: "RoadId");

            migrationBuilder.CreateIndex(
                name: "IX_Bridges_RoadRef",
                table: "Bridges",
                column: "RoadRef");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_Date",
                table: "CalendarEvents",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_RoadId",
                table: "CalendarEvents",
                column: "RoadId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstAidStepItems_StepId",
                table: "FirstAidStepItems",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_FirstAidTopicItems_TopicId",
                table: "FirstAidTopicItems",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadCauses_RoadId",
                table: "RoadCauses",
                column: "RoadId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_FromCityId",
                table: "RoadSegments",
                column: "FromCityId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadSegments_ToCityId",
                table: "RoadSegments",
                column: "ToCityId");

            migrationBuilder.CreateIndex(
                name: "IX_Tunnels_RoadRef",
                table: "Tunnels",
                column: "RoadRef");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlackSpots");

            migrationBuilder.DropTable(
                name: "Bridges");

            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "ContentSettings");

            migrationBuilder.DropTable(
                name: "DataSources");

            migrationBuilder.DropTable(
                name: "FirstAidKitItems");

            migrationBuilder.DropTable(
                name: "FirstAidStepItems");

            migrationBuilder.DropTable(
                name: "FirstAidTopicItems");

            migrationBuilder.DropTable(
                name: "HomeStats");

            migrationBuilder.DropTable(
                name: "HourWeekdayStats");

            migrationBuilder.DropTable(
                name: "HourWeights");

            migrationBuilder.DropTable(
                name: "MonthWeights");

            migrationBuilder.DropTable(
                name: "OblastMonthlyPtp");

            migrationBuilder.DropTable(
                name: "OblastPeriodStats");

            migrationBuilder.DropTable(
                name: "OblastRisks");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "RankingRows");

            migrationBuilder.DropTable(
                name: "RoadCauses");

            migrationBuilder.DropTable(
                name: "RoadRegionWeights");

            migrationBuilder.DropTable(
                name: "RoadSegments");

            migrationBuilder.DropTable(
                name: "SpeedLimits");

            migrationBuilder.DropTable(
                name: "SpeedReductionRules");

            migrationBuilder.DropTable(
                name: "TickerItems");

            migrationBuilder.DropTable(
                name: "Tunnels");

            migrationBuilder.DropTable(
                name: "WeatherDaily");

            migrationBuilder.DropTable(
                name: "WeatherHourProfiles");

            migrationBuilder.DropTable(
                name: "FirstAidSteps");

            migrationBuilder.DropTable(
                name: "FirstAidTopics");

            migrationBuilder.DropTable(
                name: "Roads");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Oblasts");

            migrationBuilder.DropTable(
                name: "WeatherLocations");
        }
    }
}
