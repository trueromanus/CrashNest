using Microsoft.AspNetCore.Mvc;

namespace CrashNest.Attributes {

    /// <summary>
    /// Which controller/endpoint consume format. Shortcut for Json.
    /// </summary>
    public class JsonInAttribute : ConsumesAttribute {

        public JsonInAttribute () : base ( "application/json" ) {
        }

    }

}
