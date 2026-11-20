using Microsoft.EntityFrameworkCore;

namespace RoadSense.Infrastructure.Sql;

public class RoadSenseDbContext(DbContextOptions<RoadSenseDbContext> options) : DbContext(options)
{
    public DbSet<CityEntity> Cities => Set<CityEntity>();
    public DbSet<RoadSegmentEntity> RoadSegments => Set<RoadSegmentEntity>();
    public DbSet<SpeedLimitEntity> SpeedLimits => Set<SpeedLimitEntity>();
    public DbSet<WeatherLocationEntity> WeatherLocations => Set<WeatherLocationEntity>();
    public DbSet<WeatherHourProfileEntity> WeatherHourProfiles => Set<WeatherHourProfileEntity>();
    public DbSet<RoadEntity> Roads => Set<RoadEntity>();
    public DbSet<BlackSpotEntity> BlackSpots => Set<BlackSpotEntity>();
    public DbSet<RoadRegionWeightEntity> RoadRegionWeights => Set<RoadRegionWeightEntity>();
    public DbSet<RoadCauseEntity> RoadCauses => Set<RoadCauseEntity>();
    public DbSet<RankingRowEntity> RankingRows => Set<RankingRowEntity>();
    public DbSet<CalendarEventEntity> CalendarEvents => Set<CalendarEventEntity>();
    public DbSet<MonthWeightEntity> MonthWeights => Set<MonthWeightEntity>();
    public DbSet<HourWeightEntity> HourWeights => Set<HourWeightEntity>();
    public DbSet<PlaceEntity> Places => Set<PlaceEntity>();
    public DbSet<TickerItemEntity> TickerItems => Set<TickerItemEntity>();
    public DbSet<HomeStatEntity> HomeStats => Set<HomeStatEntity>();
    public DbSet<ContentSettingEntity> ContentSettings => Set<ContentSettingEntity>();
    public DbSet<FirstAidStepEntity> FirstAidSteps => Set<FirstAidStepEntity>();
    public DbSet<FirstAidStepItemEntity> FirstAidStepItems => Set<FirstAidStepItemEntity>();
    public DbSet<FirstAidTopicEntity> FirstAidTopics => Set<FirstAidTopicEntity>();
    public DbSet<FirstAidTopicItemEntity> FirstAidTopicItems => Set<FirstAidTopicItemEntity>();
    public DbSet<FirstAidKitItemEntity> FirstAidKitItems => Set<FirstAidKitItemEntity>();
    public DbSet<SubscriberEntity> Subscribers => Set<SubscriberEntity>();

    public DbSet<OblastEntity> Oblasts => Set<OblastEntity>();
    public DbSet<OblastPeriodStatEntity> OblastPeriodStats => Set<OblastPeriodStatEntity>();
    public DbSet<OblastMonthlyPtpEntity> OblastMonthlyPtp => Set<OblastMonthlyPtpEntity>();
    public DbSet<HourWeekdayStatEntity> HourWeekdayStats => Set<HourWeekdayStatEntity>();
    public DbSet<OblastRiskEntity> OblastRisks => Set<OblastRiskEntity>();
    public DbSet<WeatherDailyEntity> WeatherDaily => Set<WeatherDailyEntity>();
    public DbSet<BridgeEntity> Bridges => Set<BridgeEntity>();
    public DbSet<TunnelEntity> Tunnels => Set<TunnelEntity>();
    public DbSet<SpeedReductionRuleEntity> SpeedReductionRules => Set<SpeedReductionRuleEntity>();
    public DbSet<DataSourceEntity> DataSources => Set<DataSourceEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ===== Публични данни (dataSource/processed) =====
        b.Entity<OblastEntity>(e => { e.HasKey(x => x.Name); e.Property(x => x.Name).HasMaxLength(50); });
        b.Entity<OblastPeriodStatEntity>(e =>
        {
            e.HasKey(x => new { x.Oblast, x.Period });
            e.Property(x => x.Oblast).HasMaxLength(50);
            e.Property(x => x.Period).HasMaxLength(10);
            e.HasOne<OblastEntity>().WithMany().HasForeignKey(x => x.Oblast).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<OblastMonthlyPtpEntity>(e =>
        {
            e.HasKey(x => new { x.Oblast, x.Year, x.Month });
            e.Property(x => x.Oblast).HasMaxLength(50);
            e.HasOne<OblastEntity>().WithMany().HasForeignKey(x => x.Oblast).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<HourWeekdayStatEntity>(e =>
        {
            e.HasKey(x => new { x.Weekday, x.Hour });
            e.Property(x => x.Weekday).ValueGeneratedNever();
            e.Property(x => x.Hour).ValueGeneratedNever();
        });
        b.Entity<OblastRiskEntity>(e =>
        {
            e.HasKey(x => x.Oblast);
            e.Property(x => x.Oblast).HasMaxLength(50);
            e.HasOne<OblastEntity>().WithOne().HasForeignKey<OblastRiskEntity>(x => x.Oblast).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<WeatherDailyEntity>(e =>
        {
            e.HasKey(x => new { x.Oblast, x.Date });
            e.Property(x => x.Oblast).HasMaxLength(50);
            e.HasOne<OblastEntity>().WithMany().HasForeignKey(x => x.Oblast).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<BridgeEntity>(e => { e.Property(x => x.RoadRef).HasMaxLength(100); e.HasIndex(x => x.RoadRef); });
        b.Entity<TunnelEntity>(e => { e.Property(x => x.RoadRef).HasMaxLength(100); e.HasIndex(x => x.RoadRef); });
        b.Entity<SpeedReductionRuleEntity>(e =>
        {
            e.HasKey(x => x.Condition);
            e.Property(x => x.Condition).HasMaxLength(30);
            e.Property(x => x.Advice).HasMaxLength(300);
        });
        b.Entity<DataSourceEntity>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Period).HasMaxLength(100);
            e.Property(x => x.Url).HasMaxLength(300);
        });

        b.Entity<CityEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(10);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(200);
        });

        b.Entity<RoadSegmentEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(20);
            e.Property(x => x.FromCityId).HasMaxLength(10);
            e.Property(x => x.ToCityId).HasMaxLength(10);
            e.Property(x => x.Road).HasMaxLength(20);
            e.Property(x => x.Incident).HasMaxLength(500);
            e.Property(x => x.Repair).HasMaxLength(500);
            e.HasOne(x => x.FromCity).WithMany().HasForeignKey(x => x.FromCityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToCity).WithMany().HasForeignKey(x => x.ToCityId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SpeedLimitEntity>(e =>
        {
            e.HasKey(x => new { x.RoadType, x.Vehicle });
            e.Property(x => x.RoadType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Vehicle).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<WeatherLocationEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.NearRoad).HasMaxLength(100);
            e.HasMany(x => x.HourProfiles).WithOne().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<WeatherHourProfileEntity>().HasKey(x => new { x.LocationId, x.Hour });

        b.Entity<RoadEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(20);
            e.Property(x => x.Label).HasMaxLength(100);
            e.Property(x => x.FullName).HasMaxLength(200);
            e.HasMany(x => x.BlackSpots).WithOne().HasForeignKey(x => x.RoadId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.RegionWeights).WithOne().HasForeignKey(x => x.RoadId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Causes).WithOne().HasForeignKey(x => x.RoadId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.CalendarEvents).WithOne(x => x.Road).HasForeignKey(x => x.RoadId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<BlackSpotEntity>(e =>
        {
            e.Property(x => x.RoadId).HasMaxLength(20);
            e.Property(x => x.Description).HasMaxLength(300);
        });
        b.Entity<RoadRegionWeightEntity>(e =>
        {
            e.HasKey(x => new { x.RoadId, x.Region });
            e.Property(x => x.RoadId).HasMaxLength(20);
            e.Property(x => x.Region).HasMaxLength(100);
        });
        b.Entity<RoadCauseEntity>(e =>
        {
            e.Property(x => x.RoadId).HasMaxLength(20);
            e.Property(x => x.Label).HasMaxLength(200);
        });

        b.Entity<RankingRowEntity>(e =>
        {
            e.Property(x => x.Road).HasMaxLength(100);
            e.Property(x => x.Section).HasMaxLength(200);
        });

        b.Entity<CalendarEventEntity>(e =>
        {
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Source).HasMaxLength(200);
            e.Property(x => x.RoadId).HasMaxLength(20);
            e.HasIndex(x => x.Date);
        });

        b.Entity<MonthWeightEntity>().HasKey(x => x.Month);
        b.Entity<MonthWeightEntity>().Property(x => x.Month).ValueGeneratedNever();
        b.Entity<HourWeightEntity>().HasKey(x => x.Hour);
        b.Entity<HourWeightEntity>().Property(x => x.Hour).ValueGeneratedNever();

        b.Entity<PlaceEntity>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.NearRoad).HasMaxLength(100);
        });

        b.Entity<TickerItemEntity>(e =>
        {
            e.Property(x => x.Level).HasMaxLength(10);
            e.Property(x => x.Road).HasMaxLength(100);
            e.Property(x => x.Text).HasMaxLength(300);
        });
        b.Entity<HomeStatEntity>(e =>
        {
            e.Property(x => x.Value).HasMaxLength(50);
            e.Property(x => x.Unit).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Source).HasMaxLength(300);
        });
        b.Entity<ContentSettingEntity>(e =>
        {
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
        });

        b.Entity<FirstAidStepEntity>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Lead).HasMaxLength(500);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.StepId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<FirstAidTopicEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Tag).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(200);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<FirstAidTopicItemEntity>().Property(x => x.TopicId).HasMaxLength(50);

        b.Entity<SubscriberEntity>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(254);
            e.HasIndex(x => x.Email).IsUnique();
        });
    }
}
