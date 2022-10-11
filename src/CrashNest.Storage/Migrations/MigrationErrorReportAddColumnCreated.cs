using CrashNest.Common.Attributes;
using CrashNest.Storage.Migrator;

namespace CrashNest.Storage.Migrations {

    [Migration ( 1665512669, "Adding column created to ErrorReport table" )]
    public class MigrationErrorReportAddColumnCreated : Migration {

        protected override void Up () => ExecuteCommand ( @"ALTER TABLE errorreport ADD COLUMN created timestamp(6) DEFAULT now()" );

        protected override void Down () => ExecuteCommand ( "ALTER TABLE errorreport DROP COLUMN created" );

    }
}
