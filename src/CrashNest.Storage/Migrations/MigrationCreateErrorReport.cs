using CrashNest.Common.Attributes;
using CrashNest.Storage.Migrator;

namespace CrashNest.Storage.Migrations {

    [Migration( 1659895852, "Creating ErrorReport table" )]
    public sealed class MigrationCreateErrorReport : Migration {

        protected override void Up() {
            ExecuteCommand (
                @"CREATE TABLE errorreport (
                  id uuid NOT NULL DEFAULT uuid_generate_v4 (),
                  errortype int4 NOT NULL,
                  message text NOT NULL,
                  code int4,
                  stacktrace text,
                  CONSTRAINT errorreport_pkey PRIMARY KEY (id)
                )"
            );
        }

        protected override void Down () {
            ExecuteCommand("DROP TABLE errorreport;");
        }

    }

}
