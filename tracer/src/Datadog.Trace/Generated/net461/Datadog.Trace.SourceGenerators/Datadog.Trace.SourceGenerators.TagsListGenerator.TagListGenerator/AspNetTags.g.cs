﻿// <auto-generated/>
#nullable enable

using Datadog.Trace.Processors;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.Tagging
{
    partial class AspNetTags
    {
        // AspNetRouteBytes = System.Text.Encoding.UTF8.GetBytes("aspnet.route");
        private static readonly byte[] AspNetRouteBytes = new byte[] { 97, 115, 112, 110, 101, 116, 46, 114, 111, 117, 116, 101 };
        // AspNetControllerBytes = System.Text.Encoding.UTF8.GetBytes("aspnet.controller");
        private static readonly byte[] AspNetControllerBytes = new byte[] { 97, 115, 112, 110, 101, 116, 46, 99, 111, 110, 116, 114, 111, 108, 108, 101, 114 };
        // AspNetActionBytes = System.Text.Encoding.UTF8.GetBytes("aspnet.action");
        private static readonly byte[] AspNetActionBytes = new byte[] { 97, 115, 112, 110, 101, 116, 46, 97, 99, 116, 105, 111, 110 };
        // AspNetAreaBytes = System.Text.Encoding.UTF8.GetBytes("aspnet.area");
        private static readonly byte[] AspNetAreaBytes = new byte[] { 97, 115, 112, 110, 101, 116, 46, 97, 114, 101, 97 };

        public override string? GetTag(string key)
        {
            return key switch
            {
                "aspnet.route" => AspNetRoute,
                "aspnet.controller" => AspNetController,
                "aspnet.action" => AspNetAction,
                "aspnet.area" => AspNetArea,
                _ => base.GetTag(key),
            };
        }

        public override void SetTag(string key, string value)
        {
            switch(key)
            {
                case "aspnet.route": 
                    AspNetRoute = value;
                    break;
                case "aspnet.controller": 
                    AspNetController = value;
                    break;
                case "aspnet.action": 
                    AspNetAction = value;
                    break;
                case "aspnet.area": 
                    AspNetArea = value;
                    break;
                default: 
                    base.SetTag(key, value);
                    break;
            }
        }

        public override void EnumerateTags<TProcessor>(ref TProcessor processor)
        {
            if (AspNetRoute is not null)
            {
                processor.Process(new TagItem<string>("aspnet.route", AspNetRoute, AspNetRouteBytes));
            }

            if (AspNetController is not null)
            {
                processor.Process(new TagItem<string>("aspnet.controller", AspNetController, AspNetControllerBytes));
            }

            if (AspNetAction is not null)
            {
                processor.Process(new TagItem<string>("aspnet.action", AspNetAction, AspNetActionBytes));
            }

            if (AspNetArea is not null)
            {
                processor.Process(new TagItem<string>("aspnet.area", AspNetArea, AspNetAreaBytes));
            }

            base.EnumerateTags(ref processor);
        }

        protected override void WriteAdditionalTags(System.Text.StringBuilder sb)
        {
            if (AspNetRoute is not null)
            {
                sb.Append("aspnet.route (tag):")
                  .Append(AspNetRoute)
                  .Append(',');
            }

            if (AspNetController is not null)
            {
                sb.Append("aspnet.controller (tag):")
                  .Append(AspNetController)
                  .Append(',');
            }

            if (AspNetAction is not null)
            {
                sb.Append("aspnet.action (tag):")
                  .Append(AspNetAction)
                  .Append(',');
            }

            if (AspNetArea is not null)
            {
                sb.Append("aspnet.area (tag):")
                  .Append(AspNetArea)
                  .Append(',');
            }

            base.WriteAdditionalTags(sb);
        }
    }
}
