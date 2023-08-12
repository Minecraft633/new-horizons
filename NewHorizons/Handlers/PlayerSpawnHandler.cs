using NewHorizons.Builder.General;
using NewHorizons.Utility;
using NewHorizons.Utility.OWML;
using System.Collections;
using UnityEngine;

namespace NewHorizons.Handlers
{
    public static class PlayerSpawnHandler
    {
        public static void SetUpPlayerSpawn()
        {
            var spawnPoint = Main.SystemDict[Main.Instance.CurrentStarSystem].SpawnPoint;
            if (spawnPoint != null)
            {
                SearchUtilities.Find("Player_Body").GetComponent<MatchInitialMotion>().SetBodyToMatch(spawnPoint.GetAttachedOWRigidbody());
                GetPlayerSpawner().SetInitialSpawnPoint(spawnPoint);
            }
            else
            {
                NHLogger.Log($"No NH spawn point for {Main.Instance.CurrentStarSystem}");
            }
        }

        public static void OnSystemReady(bool shouldWarpInFromShip, bool shouldWarpInFromVessel)
        {
            NHLogger.Log($"OnSystemReady {shouldWarpInFromVessel}, {shouldWarpInFromShip}, {UsingCustomSpawn()}");
            if (shouldWarpInFromShip)
            {
                Main.Instance.ShipWarpController.WarpIn(Main.Instance.WearingSuit);
            }
            else if (shouldWarpInFromVessel)
            {
                VesselWarpHandler.TeleportToVessel();
            }
            else if (UsingCustomSpawn())
            {
                InvulnerabilityHandler.MakeInvulnerable(true);

                // Idk why but these just don't work?
                var matchInitialMotion = SearchUtilities.Find("Player_Body").GetComponent<MatchInitialMotion>();
                if (matchInitialMotion != null) UnityEngine.Object.Destroy(matchInitialMotion);

                // Arbitrary number, depending on the machine some people die, some people fall through the floor, its very inconsistent
                Delay.StartCoroutine(SpawnCoroutine(30));
            }
        }

        public static void SpawnShip()
        {
            var ship = SearchUtilities.Find("Ship_Body");
            if (ship != null)
            {
                ship.SetActive(true);

                var pos = SpawnPointBuilder.ShipSpawn.transform.position;

                // Move it up a bit more when aligning to surface
                if (SpawnPointBuilder.ShipSpawnOffset != null)
                {
                    pos += SpawnPointBuilder.ShipSpawn.transform.TransformDirection(SpawnPointBuilder.ShipSpawnOffset);
                }

                SpawnBody(ship.GetAttachedOWRigidbody(), SpawnPointBuilder.ShipSpawn, pos);
            }
        }

        private static IEnumerator SpawnCoroutine(int length)
        {
            for(int i = 0; i < length; i++) 
            {
                FixPlayerVelocity();
                yield return new WaitForEndOfFrame();
            }

            InvulnerabilityHandler.MakeInvulnerable(false);

            if (!Main.Instance.IsWarpingFromShip)
            {
                if (SpawnPointBuilder.ShipSpawn != null)
                {
                    NHLogger.Log("Spawning player ship");
                    SpawnShip();
                }
                else
                {
                    NHLogger.Log("System has no ship spawn. Deactivating it.");
                    SearchUtilities.Find("Ship_Body")?.SetActive(false);
                }
            }
        }

        private static void FixPlayerVelocity()
        {
            var playerBody = SearchUtilities.Find("Player_Body").GetAttachedOWRigidbody();
            var resources = playerBody.GetComponent<PlayerResources>();

            SpawnBody(playerBody, GetDefaultSpawn());

            resources._currentHealth = 100f;
        }

        public static void SpawnBody(OWRigidbody body, SpawnPoint spawn, Vector3? positionOverride = null)
        {
            var pos = positionOverride ?? spawn.transform.position;

            body.WarpToPositionRotation(pos, spawn.transform.rotation);

            var spawnVelocity = spawn._attachedBody.GetVelocity();
            var spawnAngularVelocity = spawn._attachedBody.GetPointTangentialVelocity(pos);
            var velocity = spawnVelocity + spawnAngularVelocity;

            body.SetVelocity(velocity);
        }

        private static Vector3 CalculateMatchVelocity(OWRigidbody owRigidbody, OWRigidbody bodyToMatch, bool ignoreAngularVelocity)
        {
            var vector = Vector3.zero;
            owRigidbody.UpdateCenterOfMass();
            vector += bodyToMatch.GetVelocity();
            if (!ignoreAngularVelocity)
            {
                var worldCenterOfMass = owRigidbody.GetWorldCenterOfMass();
                var worldCenterOfMass2 = bodyToMatch.GetWorldCenterOfMass();
                var initAngularVelocity = bodyToMatch.GetAngularVelocity();
                vector += OWPhysics.PointTangentialVelocity(worldCenterOfMass, worldCenterOfMass2, initAngularVelocity);
            }

            var aoPrimary = bodyToMatch.GetComponent<AstroObject>()?._primaryBody?.GetAttachedOWRigidbody();
            // Stock sun has its primary as itself for some reason
            if (aoPrimary != null && aoPrimary != bodyToMatch)
            {
                vector += CalculateMatchVelocity(bodyToMatch, aoPrimary, true);
            }
            return vector;
        }

        public static bool UsingCustomSpawn() => Main.SystemDict[Main.Instance.CurrentStarSystem].SpawnPoint != null;
        public static PlayerSpawner GetPlayerSpawner() => GameObject.FindObjectOfType<PlayerSpawner>();
        public static SpawnPoint GetDefaultSpawn() => Main.SystemDict[Main.Instance.CurrentStarSystem].SpawnPoint ?? GetPlayerSpawner().GetSpawnPoint(SpawnLocation.TimberHearth);
    }
}
