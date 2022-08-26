using CrashNest.Common.Attributes;
using CrashNest.Storage.Migrator;

namespace CrashNest.Storage.Migrations {

    [Migration ( 1661518646, "Creating ErrorReportMetadata table" )]
    public class MigrationCreateErrorReportMetadata : Migration {

        protected override void Up () {
            ExecuteCommand (
                @"
                    CREATE TABLE ErrorReportMetadata (
                        Id uuid NOT NULL DEFAULT uuid_generate_v4 (),
                        Name text NOT NULL,
                        StringValue text,
                        IntValue int4,
                        ErrorReportId uuid,
                        CONSTRAINT ErrorReportMetadata_pkey PRIMARY KEY (id),
                        CONSTRAINT ErrorReportMetadata_ErrorReport_fkey FOREIGN KEY(ErrorReportId) REFERENCES ErrorReport(Id) ON DELETE NO ACTION ON UPDATE NO ACTION
                    )
                "
            );
        }

        protected override void Down () => ExecuteCommand ( "DROP TABLE ErrorReportMetadata" );

    }

}
