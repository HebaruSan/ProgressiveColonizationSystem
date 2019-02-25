﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FinePrint;
using UnityEngine;

namespace ProgressiveColonizationSystem
{

    public class PksSpecialStuffScrounger
        : PksTieredResourceConverter
    {
        private Waypoint waypoint;

        [KSPField(isPersistant = true)]
        private string lastId;

        [KSPEvent(guiActive = true)]
        public void FindResource()
        {
            if (!string.IsNullOrEmpty(lastId))
            {
                Guid myId = new Guid(lastId);
                Waypoint waypoint = FinePrint.WaypointManager.Instance().Waypoints.FirstOrDefault(wp => wp.navigationId == myId);
                if (waypoint != null)
                {
                    global::WaypointManager.CustomWaypoints.RemoveWaypoint(waypoint);
                }
            }

            waypoint = new Waypoint();
            waypoint.celestialName = FlightGlobals.GetHomeBody().name;
            waypoint.name = "Splut";
            waypoint.nodeCaption1 = "caption 1";
            waypoint.nodeCaption2 = "caption 2";
            waypoint.nodeCaption3 = "caption 3";

            waypoint.RandomizeNear(this.vessel.latitude, this.vessel.longitude, 20000.0 /* in meters */, waterAllowed: false, generator: new System.Random());
            waypoint.id = "custom";
            waypoint.seed = 269;

            waypoint.height = global::WaypointManager.Util.WaypointHeight(waypoint, FlightGlobals.GetHomeBody());
            waypoint.altitude = 0;
            global::WaypointManager.CustomWaypoints.AddWaypoint(waypoint);
            this.lastId = waypoint.navigationId.ToString();

            //waypoint.RandomizeNear(latitude, longitude, 100000);
            //waypoint.SetupMapNode();
            //waypoint.visible = true;


            // https://github.com/Arsonide/FinePrint/blob/daeedfe1ca7d167d255ec5b31e9c8947cb660051/Source/WaypointManager.cs#L136
            // Implies this would work:  (But it doesn't)
            // var navWaypoint = (GameObject.FindObjectOfType(typeof(NavWaypoint)) as NavWaypoint);
            //NavWaypoint navWaypoint = NavWaypoint.fetch;

            //navWaypoint.Setup(waypoint);
            //navWaypoint.Activate();
        }
    }
}
