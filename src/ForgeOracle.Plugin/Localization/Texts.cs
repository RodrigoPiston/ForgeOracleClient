using System.Collections.Generic;

namespace ForgeOracle.Plugin.Localization
{
    /// <summary>
    /// UI strings per language. To add a language, add a dictionary with the same keys and map the game's
    /// language name to it in <see cref="Localizer"/>. Missing keys fall back to English.
    /// </summary>
    internal static class Texts
    {
        public static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["compact.success"] = "NEXT UPGRADE: SUCCESS",
            ["compact.destroyed"] = "NEXT UPGRADE: ITEM DESTROYED",
            ["compact.levelloss"] = "NEXT UPGRADE: LEVEL -{0}",
            ["compact.nochange"] = "NEXT UPGRADE: NO CHANGE",
            ["compact.unavailable"] = "PREDICTION UNAVAILABLE",
            ["compact.modified"] = "PREDICTION UNAVAILABLE — MODIFIED FORGE ALGORITHM",
            ["outcome.success"] = "SUCCESS",
            ["outcome.destroyed"] = "ITEM DESTROYED",
            ["outcome.levelloss"] = "LEVEL -{0}",
            ["outcome.nochange"] = "NO CHANGE",
            ["outcome.unknown"] = "UNKNOWN",
            ["confidence.Verified"] = "VERIFIED",
            ["confidence.Unverified"] = "UNVERIFIED",
            ["confidence.Unsupported"] = "UNSUPPORTED",
            ["panel.title"] = "FORGE ORACLE",
            ["panel.target"] = "Target level: +{0}",
            ["panel.next"] = "NEXT ATTEMPT",
            ["panel.roll"] = "Roll: {0}",
            ["panel.threshold"] = "Threshold: {0}",
            ["panel.break"] = "Break threshold: {0}",
            ["panel.confidence"] = "Confidence: {0}",
            ["panel.provider"] = "Provider: {0}",
            ["panel.seed.Stored"] = "Seed: stored on character",
            ["panel.seed.DerivedFromName"] = "Seed: first attempt (from character name)",
            ["panel.forecast.RepeatedAttempts"] = "NEXT ATTEMPTS AT +{0}",
            ["panel.forecast.Progression"] = "LEVEL BY LEVEL FROM +{0}",
            ["panel.forecast.conditional"] = "only if the earlier step succeeds",
            ["panel.forecast.skilldrift"] = "Later rows assume your current forging skill.",
            ["panel.mode"] = "[switch view]",
            ["panel.expand"] = "[+] forecast",
            ["panel.collapse"] = "[−] forecast",
            ["tooltip.RepeatedAttempts"] = "What the next attempts at this exact level would give, " +
                "one after another, with any copy of this item. " +
                "Rows after the first only happen if you try again at this level.",
            ["tooltip.Progression"] = "The first attempt at each higher level, assuming every earlier step succeeds. " +
                "Rows after a predicted failure are hypothetical: the item would not reach them.",
            ["reason.NoUpgraderResource"] = "This recipe has no Forge of Potential idol.",
            ["reason.MissingPlayerName"] = "The character name is not available yet.",
            ["reason.MissingThresholds"] = "The forge chances could not be read.",
            ["reason.ModifiedAlgorithm"] = "Another mod replaces the forge roll.",
            ["reason.RngMismatch"] = "The RNG self-test failed on this game version.",
            ["reason.ProviderError"] = "Prediction failed (see the BepInEx log).",
            ["reason.Stale"] = "Attempt made; select the item again or run 'forgeoracle refresh'.",
            ["warn.unverified"] = "Not verified: {0}",
            ["warn.rng"] = "RNG self-test did not pass",
            ["confirm.destroy"] =
                "Forge Oracle: the next attempt is predicted to DESTROY this item. Click again to proceed.",
        };

        public static readonly Dictionary<string, string> Spanish = new Dictionary<string, string>
        {
            ["compact.success"] = "PRÓXIMA MEJORA: ÉXITO",
            ["compact.destroyed"] = "PRÓXIMA MEJORA: OBJETO DESTRUIDO",
            ["compact.levelloss"] = "PRÓXIMA MEJORA: NIVEL -{0}",
            ["compact.nochange"] = "PRÓXIMA MEJORA: SIN CAMBIOS",
            ["compact.unavailable"] = "PREDICCIÓN NO DISPONIBLE",
            ["compact.modified"] = "PREDICCIÓN NO DISPONIBLE — ALGORITMO DE FORJA MODIFICADO",
            ["outcome.success"] = "ÉXITO",
            ["outcome.destroyed"] = "OBJETO DESTRUIDO",
            ["outcome.levelloss"] = "NIVEL -{0}",
            ["outcome.nochange"] = "SIN CAMBIOS",
            ["outcome.unknown"] = "DESCONOCIDO",
            ["confidence.Verified"] = "VERIFICADA",
            ["confidence.Unverified"] = "SIN VERIFICAR",
            ["confidence.Unsupported"] = "NO COMPATIBLE",
            ["panel.title"] = "ORÁCULO DE LA FORJA",
            ["panel.target"] = "Nivel objetivo: +{0}",
            ["panel.next"] = "PRÓXIMO INTENTO",
            ["panel.roll"] = "Tirada: {0}",
            ["panel.threshold"] = "Umbral: {0}",
            ["panel.break"] = "Umbral de rotura: {0}",
            ["panel.confidence"] = "Confianza: {0}",
            ["panel.provider"] = "Proveedor: {0}",
            ["panel.seed.Stored"] = "Semilla: guardada en el personaje",
            ["panel.seed.DerivedFromName"] = "Semilla: primer intento (del nombre del personaje)",
            ["panel.forecast.RepeatedAttempts"] = "PRÓXIMOS INTENTOS A +{0}",
            ["panel.forecast.Progression"] = "NIVEL POR NIVEL DESDE +{0}",
            ["panel.forecast.conditional"] = "solo si el paso anterior tiene éxito",
            ["panel.forecast.skilldrift"] = "Las filas siguientes suponen tu habilidad de forja actual.",
            ["panel.mode"] = "[cambiar vista]",
            ["panel.expand"] = "[+] pronóstico",
            ["panel.collapse"] = "[−] pronóstico",
            ["tooltip.RepeatedAttempts"] = "Lo que darían los próximos intentos a este mismo nivel, uno tras otro, " +
                "con cualquier copia de este objeto. Las filas siguientes solo ocurren si vuelves a intentarlo.",
            ["tooltip.Progression"] = "El primer intento en cada nivel superior, suponiendo que cada paso anterior " +
                "tiene éxito. Tras un fallo previsto las filas son hipotéticas: el objeto no llegaría a ellas.",
            ["reason.NoUpgraderResource"] = "Esta receta no tiene ídolo de la Forja del Potencial.",
            ["reason.MissingPlayerName"] = "El nombre del personaje aún no está disponible.",
            ["reason.MissingThresholds"] = "No se pudieron leer las probabilidades de la forja.",
            ["reason.ModifiedAlgorithm"] = "Otro mod reemplaza la tirada de la forja.",
            ["reason.RngMismatch"] = "La autoprueba del RNG falló en esta versión del juego.",
            ["reason.ProviderError"] = "La predicción falló (ver el registro de BepInEx).",
            ["reason.Stale"] = "Intento realizado; vuelve a elegir el objeto o usa 'forgeoracle refresh'.",
            ["warn.unverified"] = "Sin verificar: {0}",
            ["warn.rng"] = "la autoprueba del RNG no pasó",
            ["confirm.destroy"] = "Oráculo de la Forja: el próximo intento DESTRUIRÁ este objeto. " +
                "Haz clic otra vez para continuar.",
        };
    }
}
