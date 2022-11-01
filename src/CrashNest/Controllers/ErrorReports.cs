using CrashNest.Attributes;
using CrashNest.Common.Domain;
using CrashNest.Common.RequestModels;
using CrashNest.Common.Services;
using CrashNest.Common.Storage;
using Microsoft.AspNetCore.Mvc;
using SqlKata;

namespace CrashNest.Controllers {

    [ApiController, JsonIn, JsonOut, Route ( "/api/reports" )]
    public class ErrorReports {

        private readonly IStorageContext m_storageContext;

        private readonly INotificationRuleService m_notificationRuleService;

        public ErrorReports ( IStorageContext storageContext, INotificationRuleService notificationRuleService ) {
            m_storageContext = storageContext ?? throw new ArgumentNullException ( nameof ( storageContext ) );
            m_notificationRuleService = notificationRuleService ?? throw new ArgumentNullException ( nameof ( notificationRuleService ) );
        }

        [HttpPost ( "save" )]
        public Task Save ( [FromBody, RequiredParameter] RegisterReportModel model ) {
            if ( model == null ) throw new ArgumentNullException ( nameof ( model ) );

            return m_storageContext.MakeInTransaction (
                async () => {
                    model.Report.Id = Guid.Empty;
                    model.Report.Created = DateTime.UtcNow;

                    await m_storageContext.AddOrUpdate ( model.Report );

                    foreach ( var metadata in model.Metadata ) {
                        metadata.Id = Guid.Empty;
                        metadata.ErrorReportId = model.Report.Id;
                    }
                    await m_storageContext.MultiAddOrUpdate ( model.Metadata );

                    await m_notificationRuleService.SendNotificationIfMetConditions ( model.Report, model.Metadata );
                }
            );
        }

        [HttpPost ( "byfilter" )]
        public async Task<IEnumerable<ErrorReport>> ByFilter ( [FromBody, RequiredParameter] ReportFilterListModel model ) {
            if ( model == null ) throw new ArgumentNullException ( nameof ( model ) );

            var query = new Query ( nameof ( ErrorReport ) );

            ApplyFilters ( model, query );

            return await m_storageContext.GetAsync<ErrorReport> ( query );
        }

        [HttpPost ( "withmetadata/byfilter" )]
        public async Task<IEnumerable<ErrorReportWithMetadataModel>> WithMetadataByFilter ( [FromBody, RequiredParameter] ReportFilterListWithMetadataModel model ) {
            if ( model == null ) throw new ArgumentNullException ( nameof ( model ) );

            var query = new Query ( nameof ( ErrorReport ) );
            ApplyFilters ( model, query );
            var reports = await m_storageContext.GetAsync<ErrorReportWithMetadataModel> ( query );

            var metadataQuery = new Query ( nameof ( ErrorReportMetadata ) );
            metadataQuery
                .WhereIn ( "errorreportid", reports.Select ( a => a.Id ).ToList () )
                .OrderBy ( "errorreportid" );
            var metadata = await m_storageContext.GetAsync<ErrorReportMetadata> ( metadataQuery );

            if ( metadata.Any () ) MapMetadataToReports ( model, reports, metadata );

            return reports;
        }

        private static void ApplyFilters ( ReportFilterListModel model, Query query ) {
            FillDateRange ( model, query );
            if ( model.Codes.Any () ) query.WhereIn ( "code", model.Codes );
            if ( model.ErrorTypes.Any () ) query.WhereIn ( "errortype", model.ErrorTypes );
            if ( !string.IsNullOrEmpty ( model.Message ) ) query.WhereLike ( "message", model.Message );

            if ( FillStringMetadataFilters ( model, query ) || FillNumberMetadataFilters ( model, query ) ) {
                query.Join ( "errorreportmetadata", "errorreportmetadata.errorreportid", "errorreport.id" );
                query.SelectRaw ( "DISTINCT errorreport.*" );
            }
        }

        private static void MapMetadataToReports ( ReportFilterListWithMetadataModel model, IEnumerable<ErrorReportWithMetadataModel> reports, IEnumerable<ErrorReportMetadata> metadata ) {
            Guid currentErrorReport = metadata.First ().ErrorReportId;
            var report = reports.First ( a => a.Id == currentErrorReport );
            var duplicatesDictionary = new Dictionary<string, uint> ();
            var isNeedFilteringFields = model.IncludedFields?.Any () ?? false;
            foreach ( var metadataItem in metadata ) {
                if ( currentErrorReport != metadataItem.ErrorReportId ) {
                    currentErrorReport = metadataItem.ErrorReportId;
                    report = reports.First ( a => a.Id == currentErrorReport );
                }

                if ( isNeedFilteringFields && model.IncludedFields != null && !model.IncludedFields.Contains ( metadataItem.Name ) ) continue;

                object? value = null;
                if ( metadataItem.StringValue != null ) value = metadataItem.StringValue;
                if ( metadataItem.IntValue != null ) value = metadataItem.IntValue;

                if ( value != null ) {
                    if ( report.Metadata.ContainsKey ( metadataItem.Name ) ) {
                        var suffix = "";
                        if ( duplicatesDictionary.TryGetValue ( metadataItem.Name, out var counts ) ) {
                            duplicatesDictionary[metadataItem.Name] += 1;
                            suffix = $"_{duplicatesDictionary[metadataItem.Name]}";
                        } else {
                            duplicatesDictionary[metadataItem.Name] = 1;
                            suffix = "_1";
                        }
                        report.Metadata.Add ( metadataItem.Name + suffix, value );
                    } else {
                        report.Metadata.Add ( metadataItem.Name, value );
                    }
                }
            }
        }

        private static void FillDateRange ( ReportFilterListModel model, Query query ) {
            if ( model.StartDate.HasValue && model.EndDate.HasValue ) {
                query
                    .Where ( "created", ">=", model.StartDate.Value )
                    .Where ( "created", "<=", model.EndDate.Value );
            } else {
                switch ( model.ListMode ) {
                    case ReportFilterListMode.LastFiveMinutes:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddMinutes ( -5 ) );
                        break;
                    case ReportFilterListMode.LastHour:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddHours ( -1 ) );
                        break;
                    case ReportFilterListMode.LastDay:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddDays ( -1 ) );
                        break;
                    case ReportFilterListMode.LastWeek:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddDays ( -7 ) );
                        break;
                    case ReportFilterListMode.LastMonth:
                        query.Where ( "created", ">=", DateTime.UtcNow.AddDays ( -31 ) );
                        break;
                    default: throw new NotSupportedException ( $"List mode {model.ListMode} not supported!" );
                }
            }
        }

        private static bool FillStringMetadataFilters ( ReportFilterListModel model, Query query ) {
            var hasFilters = false;
            foreach ( var filter in model.MetadataStringFilters ) {
                query
                    .WhereLike ( "errorreportmetadata.name", filter.Name )
                    .WhereLike ( "errorreportmetadata.stringvalue", filter.Value );
                if ( !hasFilters ) hasFilters = true;
            }

            return hasFilters;
        }

        private static bool FillNumberMetadataFilters ( ReportFilterListModel model, Query query ) {
            var hasFilters = false;

            foreach ( var filter in model.MetadataNumberFilters ) {
                if ( !( filter.Value.HasValue || filter.Start.HasValue && filter.End.HasValue ) ) continue;

                query.WhereLike ( "errorreportmetadata.name", filter.Name );
                if ( filter.Value.HasValue ) {
                    query.Where ( "errorreportmetadata.intvalue", filter.Value.Value );

                    if ( !hasFilters ) hasFilters = true;
                }
                if ( filter.Start.HasValue && filter.End.HasValue ) {
                    query.Where ( "errorreportmetadata.intvalue", ">=", filter.Start.Value );
                    query.Where ( "errorreportmetadata.intvalue", "<=", filter.End.Value );

                    if ( !hasFilters ) hasFilters = true;
                }
            }

            return hasFilters;
        }

    }

}
