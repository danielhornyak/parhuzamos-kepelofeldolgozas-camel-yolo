using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;
using System;

namespace ParallelPreprocessing.Tests
{
    /// <summary>
    /// Egységtesztek a képelőfeldolgozási pipeline három alapvető lépésére:
    /// CropStep (vágás), ResizeStep (átméretezés bilineáris interpolációval),
    /// NormalizeStep (byte → float [0,1] normalizálás).
    /// Ezek a tesztek garantálják, hogy a soros referencia-implementáció
    /// helyes, mielőtt a párhuzamos modellekkel összevetnénk.
    /// </summary>
    [TestClass]
    public class PreprocessingTests
    {
        // ============================================================
        // TESZT 1: CropStep — felső vágási logika ellenőrzése
        // ============================================================
        // Cél: Verifikálni, hogy a CropStep helyesen vágja le a kép
        //      felső 30%-át, és a megmaradó képkocka méretei,
        //      illetve pixel tömb hossza konzisztensek maradnak.
        //
        // Tesztelt képlet (4.1): newH = H - floor(0.30 * H)
        //                        100x100 bemenet → 100x70 kimenet
        //
        // Mit verifikálunk:
        //   - A szélesség (Width) NEM változik (csak függőleges vágás)
        //   - A magasság (Height) pontosan 30%-kal csökken
        //   - A PixelData tömb új mérete: width * newHeight * 3 (RGB)
        //   - Az eredmény objektum és a pixel tömb nem null
        // ============================================================
        [TestMethod]
        public void CropStep_30Percent_CalculatesCorrectDimensions()
        {
            // ---- Arrange (Előkészítés) ----
            // 100x100-as tesztkép létrehozása (a 10x10-es túl kicsi
            // lenne, és bizonyos implementációkban kerekítési hibákat okozhat)
            int width = 100;
            int height = 100;
            byte[] dummyPixels = new byte[width * height * 3]; // RGB → 3 byte/pixel

            // A tömböt nem-nulla értékkel töltjük fel, hogy elkerüljük
            // az esetleges "üres/fekete kép" validációs hibákat
            // a CropStep belsejében
            Array.Fill(dummyPixels, (byte)128);

            var inputFrame = new FrameData
            {
                Width = width,
                Height = height,
                PixelData = dummyPixels
            };
            var cropStep = new CropStep(0.30f); // 30%-os felső vágás

            // ---- Act (Végrehajtás) ----
            // A vágási lépés futtatása a tesztképen
            var resultFrame = cropStep.Process(inputFrame);

            // ---- Assert (Verifikáció) ----
            // Várt magasság: 100 - (100 * 0.30) = 70 pixel
            int expectedHeight = (int)(height * (1 - 0.30f));

            // Alapvető nem-null ellenőrzések — érthető hibaüzenet,
            // ha az implementáció null-t adna vissza
            Assert.IsNotNull(resultFrame, "A vágás eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.PixelData, "A pixel adatnak léteznie kell.");

            // A vágás csak a magasságot érinti, a szélesség változatlan
            Assert.AreEqual(width, resultFrame.Width,
                "A szélességnek változatlannak kell maradnia.");

            // A magasság pontosan a megadott százalékkal csökken
            Assert.AreEqual(expectedHeight, resultFrame.Height,
                "A magasságnak pontosan 30%-kal kell csökkennie.");

            // A pixel tömb új mérete konzisztens az új dimenziókkal
            Assert.HasCount(width * expectedHeight * 3, resultFrame.PixelData,
                "A pixel tömb mérete hibás.");
        }

        // ============================================================
        // TESZT 2: ResizeStep — bilineáris interpoláció helyessége
        // ============================================================
        // Cél: Verifikálni, hogy a bilineáris interpoláció során
        //      a kimeneti dimenziók pontosak, a pixel tömb mérete
        //      megfelelő, és a színinformáció megmarad.
        //
        // Tesztelt képlet (4.2): bilineáris interpoláció
        //                        4x4 piros bemenet → 2x2 piros kimenet
        //
        // Miért 4x4 → 2x2 (és nem 2x2 → 1x1)?
        //   A bilineáris interpoláció (srcDim-1)/(targetDim-1) képletet
        //   használhat — ha targetDim=1, akkor 0/0 osztás = NaN/exception.
        //   A 4x4 → 2x2 átméretezés ezt elkerüli.
        //
        // Mit verifikálunk:
        //   - A kimeneti szélesség és magasság a kért célméret
        //   - A pixel tömb hossza: targetW * targetH * 3
        //   - Egységes piros bemenetből egységes piros kimenet
        //     keletkezik (kerekítési tűréssel: R≥250, G≤5, B≤5)
        // ============================================================
        [TestMethod]
        public void ResizeStep_BilinearInterpolation_ScalesCorrectly()
        {
            // ---- Arrange ----
            int srcWidth = 4;
            int srcHeight = 4;
            int targetWidth = 2;
            int targetHeight = 2;

            // 4x4-es tesztkép, ahol minden pixel tiszta piros (R=255, G=0, B=0).
            // Egységes szín esetén az interpoláció eredménye is azonos szín
            // kell legyen — ez ellenőrzi, hogy a súlyozási képlet helyes.
            byte[] pixels = new byte[srcWidth * srcHeight * 3];
            for (int i = 0; i < pixels.Length; i += 3)
            {
                pixels[i] = 255;     // R csatorna
                pixels[i + 1] = 0;   // G csatorna
                pixels[i + 2] = 0;   // B csatorna
            }

            var inputFrame = new FrameData
            {
                Width = srcWidth,
                Height = srcHeight,
                PixelData = pixels
            };
            var resizeStep = new ResizeStep(targetWidth, targetHeight);

            // ---- Act ----
            // Az átméretezés végrehajtása 4x4 → 2x2 felbontásra
            var resultFrame = resizeStep.Process(inputFrame);

            // ---- Assert ----
            // Nem-null ellenőrzések
            Assert.IsNotNull(resultFrame, "Az átméretezés eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.PixelData, "A pixel adatnak léteznie kell.");

            // A kimeneti dimenziók egyezzenek a kért célmérettel
            Assert.AreEqual(targetWidth, resultFrame.Width,
                "A célszélesség hibásan lett kiszámítva.");
            Assert.AreEqual(targetHeight, resultFrame.Height,
                "A célmagasság hibásan lett kiszámítva.");

            // A kimeneti pixel tömb mérete: 2 * 2 * 3 = 12 byte
            Assert.HasCount(targetWidth * targetHeight * 3, resultFrame.PixelData,
                "A kimeneti tömb hossza érvénytelen.");

            // Színhelyesség verifikálása minden kimeneti pixelre.
            // Egységes piros bemenetből (255,0,0) az interpoláció során
            // is piros kell maradjon — kerekítési hibákra tűréshatár:
            //   R ≥ 250 (nem lehet sokkal kisebb 255-nél)
            //   G ≤ 5   (nem lehet sokkal nagyobb 0-nál)
            //   B ≤ 5   (nem lehet sokkal nagyobb 0-nál)
            for (int i = 0; i < resultFrame.PixelData.Length; i += 3)
            {
                Assert.IsGreaterThanOrEqualTo(250, resultFrame.PixelData[i],
                    $"R csatorna interpolációs hiba a {i} indexnél.");
                Assert.IsLessThanOrEqualTo(5, resultFrame.PixelData[i + 1],
                    $"G csatorna interpolációs hiba a {i + 1} indexnél.");
                Assert.IsLessThanOrEqualTo(5, resultFrame.PixelData[i + 2],
                    $"B csatorna interpolációs hiba a {i + 2} indexnél.");
            }
        }

        // ============================================================
        // TESZT 3: NormalizeStep — byte → float konverzió
        // ============================================================
        // Cél: Verifikálni, hogy a normalizálás korrekt módon konvertálja
        //      a [0..255] tartományú byte értékeket [0.0..1.0] tartományú
        //      float értékekké, és felszabadítja a nyers byte tömböt
        //      a memória takarékos kezelése érdekében.
        //
        // Tesztelt képlet (4.3): float[i] = byte[i] / 255.0f
        //
        // Tesztpontok:
        //   0   → 0.000f  (alsó határ)
        //   127 → 0.498f  (~középérték, kerekítési viselkedés)
        //   255 → 1.000f  (felső határ)
        //
        // Mit verifikálunk:
        //   - A NormalizedData tömb létezik és nem null
        //   - A tömb hossza egyezik a bemeneti pixel számmal
        //   - A normalizált értékek pontosak (0.001 tűréssel)
        //   - A nyers PixelData tömb felszabadult (null vagy üres)
        //     → ez memóriaoptimalizációs követelmény, hogy a GC
        //       hamarabb felszabadíthassa a már nem szükséges adatot
        // ============================================================
        [TestMethod]
        public void NormalizeStep_ConvertsByteToFloatCorrectly()
        {
            // ---- Arrange ----
            // Három tesztpont: alsó határ (0), középérték (127), felső határ (255)
            byte[] pixels = new byte[] { 0, 127, 255 };
            var inputFrame = new FrameData
            {
                Width = 1,
                Height = 1,
                PixelData = pixels
            };
            var normalizeStep = new NormalizeStep();

            // ---- Act ----
            var resultFrame = normalizeStep.Process(inputFrame);

            // ---- Assert ----
            // Alapvető szerkezeti ellenőrzések
            Assert.IsNotNull(resultFrame, "A normalizálás eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.NormalizedData,
                "A normalizált adat nem lehet null.");
            Assert.HasCount(3, resultFrame.NormalizedData,
                "A normalizált tömb hossza nem egyezik a bemenettel.");

            // Numerikus pontosság ellenőrzése (0.001 tűrés a float-aritmetikára)
            Assert.AreEqual(0.0f, resultFrame.NormalizedData[0], 0.001f,
                "0 byte → 0.0f normalizálási hiba.");
            Assert.AreEqual(0.498f, resultFrame.NormalizedData[1], 0.001f,
                "127 byte → ~0.498f normalizálási hiba (127/255).");
            Assert.AreEqual(1.0f, resultFrame.NormalizedData[2], 0.001f,
                "255 byte → 1.0f normalizálási hiba.");

            // Memóriafelszabadítás verifikálása.
            // Az implementáció kétféleképpen jelezheti a felszabadítást:
            //   1. PixelData = null            → klasszikus referencia-elengedés
            //   2. PixelData = Array.Empty<byte>() → biztonságosabb, null-mentes minta
            // Mindkét megoldást elfogadjuk érvényesnek.
            bool isReleased = resultFrame.PixelData == null
                              || resultFrame.PixelData.Length == 0;
            Assert.IsTrue(isReleased,
                "A nyers byte tömböt fel kellett volna szabadítani " +
                "(null vagy üres tömb) a GC tehermentesítése érdekében.");
        }
    }
}