using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;
using ParallelPreprocessing.Processing;

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

            // Bemeneti képkocka összeállítása a tesztadatokkal
            var inputFrame = new FrameData
            {
                Width = width,
                Height = height,
                PixelData = dummyPixels
            };

            // A vizsgált CropStep példányosítása 30%-os felső vágással
            var cropStep = new CropStep(0.30f);

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
            // Forrás- és célméretek definiálása
            int srcWidth = 4;
            int srcHeight = 4;
            int targetWidth = 2;
            int targetHeight = 2;

            // 4x4-es tesztkép, ahol minden pixel tiszta piros (R=255, G=0, B=0).
            // Egységes szín esetén az interpoláció eredménye is azonos szín
            // kell legyen — ez ellenőrzi, hogy a súlyozási képlet helyes.
            byte[] pixels = new byte[srcWidth * srcHeight * 3];

            // Pixelenként végigiterálunk és beállítjuk a piros színt
            // (3 byte-ot lépünk: R, G, B csatornák egy pixelben)
            for (int i = 0; i < pixels.Length; i += 3)
            {
                pixels[i] = 255;     // R csatorna — maximális piros
                pixels[i + 1] = 0;   // G csatorna — nincs zöld
                pixels[i + 2] = 0;   // B csatorna — nincs kék
            }

            // Bemeneti képkocka létrehozása a tesztképpel
            var inputFrame = new FrameData
            {
                Width = srcWidth,
                Height = srcHeight,
                PixelData = pixels
            };

            // A vizsgált ResizeStep példányosítása a célmérettel
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
                // R csatorna közel maximális kell legyen
                Assert.IsGreaterThanOrEqualTo(250, resultFrame.PixelData[i],
                    $"R csatorna interpolációs hiba a {i} indexnél.");

                // G csatorna közel nulla kell legyen
                Assert.IsLessThanOrEqualTo(5, resultFrame.PixelData[i + 1],
                    $"G csatorna interpolációs hiba a {i + 1} indexnél.");

                // B csatorna közel nulla kell legyen
                Assert.IsLessThanOrEqualTo(5, resultFrame.PixelData[i + 2],
                    $"B csatorna interpolációs hiba a {i + 2} indexnél.");
            }
        }

        // TESZT 3: NormalizeStep — byte → float konverzió + BGR→RGB swap
        // ============================================================
        // Cél: Verifikálni, hogy a normalizálás korrekt módon konvertálja
        //      a [0..255] tartományú byte értékeket [0.0..1.0] tartományú
        //      float értékekké, egyidejűleg BGR→RGB csatornacserét végez,
        //      és felszabadítja a nyers byte tömböt.
        //
        // A PixelData BGR-ben érkezik (a loader CvtColor nélkül másol).
        // A NormalizeStep csere: normalized[0]=R, normalized[1]=G, normalized[2]=B.
        //
        // Tesztpixel (1×1, BGR): B=0, G=127, R=255
        // Várható RGB output:
        //   normalized[0] = R/255 = 255/255 = 1.000f
        //   normalized[1] = G/255 = 127/255 ≈ 0.498f
        //   normalized[2] = B/255 =   0/255 = 0.000f
        //
        // Mit verifikálunk:
        //   - A NormalizedData tömb létezik és nem null
        //   - A tömb hossza egyezik a bemeneti pixel számmal
        //   - A normalizált értékek pontosak és BGR→RGB sorrendben vannak (0.001 tűréssel)
        //   - A nyers PixelData tömb felszabadult (null vagy üres)
        // ============================================================
        [TestMethod]
        public void NormalizeStep_ConvertsByteToFloatCorrectly()
        {
            // ---- Arrange ----
            // 1×1 pixel BGR sorrendben: B=0, G=127, R=255
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
            Assert.IsNotNull(resultFrame, "A normalizálás eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.NormalizedData, "A normalizált adat nem lehet null.");
            Assert.HasCount(3, resultFrame.NormalizedData, "A normalizált tömb hossza nem egyezik a bemenettel.");

            // BGR→RGB swap után: [0]=R=1.0, [1]=G≈0.498, [2]=B=0.0
            Assert.AreEqual(1.0f, resultFrame.NormalizedData[0], 0.001f,
                "BGR→RGB: R csatorna (255 byte → 1.0f) hiba.");

            // Javítva: 0.498f az elvárt érték, és a [1]-es index a G csatorna
            Assert.AreEqual(0.498f, resultFrame.NormalizedData[1], 0.001f,
                "BGR→RGB: G csatorna (127 byte → ~0.498f) hiba.");

            Assert.AreEqual(0.0f, resultFrame.NormalizedData[2], 0.001f,
                "BGR→RGB: B csatorna (0 byte → 0.0f) hiba.");

            bool isReleased = resultFrame.PixelData == null || resultFrame.PixelData.Length == 0;
            Assert.IsTrue(isReleased, "A nyers byte tömböt fel kellett volna szabadítani...");
        }

            // ============================================================
            // TESZT 4: Konzisztencia — Work Pool vs. soros eredmény
            // ============================================================
            // Cél: Verifikálni, hogy a párhuzamos WorkPoolProcessor
            //      bit-szinten azonos eredményt ad, mint a soros referencia.
            //      Ez kulcsfontosságú szálbiztossági teszt: ha bármelyik
            //      pixel értéke eltér, az race condition-re vagy hibás
            //      megosztott állapotra utal.
            //
            // Mit verifikálunk:
            //   - A két modell ugyanannyi képkockát ad vissza
            //   - Frame-enként a NormalizedData tömbök BIT-PONTOSAN egyeznek
            //   - A frame-ek sorrend-független módon összevethetők
            //     (a Work Pool eltérő sorrendben végezhet, ezért
            //      FrameIndex alapján párosítunk)
            // ============================================================
            [TestMethod]
        public void Consistency_WorkPool_ProducesBitIdenticalResultToSequential()
        {
            // ---- Arrange ----
            // Tesztképkockák mérete (320x240, közepes felbontás)
            int width = 320;
            int height = 240;

            // Pipeline példányosítása a célparaméterekkel:
            //   640x480 célfelbontás, 30%-os felső vágás
            var pipeline = new PreprocessingPipeline(640, 480, 0.3f);

            // 5 darab teszt képkocka generálása véletlen pixelértékekkel.
            // Fix seed (42) használata → a teszt determinisztikus,
            // minden futtatáskor ugyanaz a bemeneti adat.
            var frames = new List<FrameData>();
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                // Új byte tömb a képkocka számára (RGB → 3 byte/pixel)
                byte[] data = new byte[width * height * 3];

                // A tömb feltöltése pszeudo-véletlen byte-okkal
                rnd.NextBytes(data);

                // Frame hozzáadása a listához egyedi indexszel
                frames.Add(new FrameData
                {
                    FrameIndex = i,
                    Width = width,
                    Height = height,
                    PixelData = data
                });
            }

            // ---- Act ----
            // A bemeneti képkockákat először a SOROS referencián futtatjuk
            // → ez adja az "igazság-mércét" (ground truth)
            var sequentialResults = SequentialProcessor.Process(frames, pipeline);

            // Ezután ugyanazokat a képkockákat a párhuzamos
            // WorkPoolProcessor-on is végigfuttatjuk
            var workPoolResults = WorkPoolProcessor.Process(frames, pipeline);

            // ---- Assert ----
            // Először a darabszám egyezését ellenőrizzük — ha bármelyik
            // képkocka "elveszne" a párhuzamosítás során, ez azonnal kiderül
            Assert.HasCount(sequentialResults.Length, workPoolResults,
                "A kimeneti darabszám eltér.");

            // Frame-enkénti összehasonlítás
            for (int i = 0; i < sequentialResults.Length; i++)
            {
                // A soros eredmény közvetlenül indexelhető (sorrendtartó)
                var seqFrame = sequentialResults[i];

                // A Work Pool eredménye lehet kevert sorrendű, ezért
                // a megfelelő frame-et FrameIndex alapján keressük meg
                var poolFrame = workPoolResults.First(f => f.FrameIndex == i);

                // Mindkét frame normalizált adata kell létezzen,
                // különben a normalizálási lépés sérült
                Assert.IsNotNull(seqFrame.NormalizedData);
                Assert.IsNotNull(poolFrame.NormalizedData);

                // Bit-szintű (pixel-szintű) összehasonlítás:
                // a normalizált float értékeknek PONTOSAN egyezniük kell.
                // Itt nem használunk tűréshatárt, mert ugyanazon
                // determinisztikus algoritmus eredményét várjuk —
                // bármilyen eltérés szálbiztossági hibára utal!
                for (int j = 0; j < seqFrame.NormalizedData.Length; j++)
                {
                    if (seqFrame.NormalizedData[j] != poolFrame.NormalizedData[j])
                    {
                        // Pontos hibalokalizálás: melyik frame, melyik pixel
                        Assert.Fail(
                            $"Eltérés a {i}. frame {j}. pixelénél! " +
                            "Szálbiztossági hiba.");
                    }
                }
            }
        }

        // ============================================================
        // TESZT 5: Memória — nincs szivárgás nagy köteg után
        // ============================================================
        // Cél: Verifikálni, hogy a WorkPoolProcessor nem szivárogtat
        //      memóriát nagy mennyiségű képkocka feldolgozása után.
        //      Egy hibás implementáció (pl. statikus listában tárolt
        //      referenciák, le nem zárt CancellationToken, fel nem
        //      szabadított ConcurrentBag) jelentős memóriaszivárgáshoz
        //      vezethet hosszú futtatás során.
        //
        // Módszertan:
        //   1. BEMELEGÍTÉS — kis köteggel előmelegítjük a futási
        //      környezetet (JIT, thread pool, TLS pufferek).
        //      Ezeknek az allokációi NORMÁLISAK, nem szivárgások,
        //      de torzítanák a mérést.
        //   2. ALAPHELYZET — a bemelegítés UTÁN GC + memóriamérés.
        //   3. NAGY TERHELÉS — 50 képkocka feldolgozása
        //      (~184 MB allokáció normál esetben).
        //   4. MÉRÉS — GC + memóriamérés, és ellenőrzés, hogy
        //      a különbség egy ésszerű küszöb alatt marad-e.
        //
        // Mit verifikálunk:
        //   - A nagy köteg feldolgozás után a heap memória nem
        //     növekedett 50 MB-nál többet → nincs szivárgás.
        // ============================================================
        [TestMethod]
        public void Memory_WorkPool_NoLeakAfterLargeBatch()
        {
            // ---- Arrange ----
            // Pipeline példányosítása (a teszt szempontjából a paraméterek
            // konkrét értéke nem fontos, csak az hogy működjön)
            var pipeline = new PreprocessingPipeline(640, 480, 0.3f);

            // Lambda-ba kiszervezett feldolgozási logika, hogy ugyanazt
            // a műveletsort kétszer is meghívhassuk (bemelegítéshez +
            // valódi teszthez) kódduplikáció nélkül
            Action<int> processBatch = (frameCount) =>
            {
                // Adott számú teszt képkocka előállítása
                var frames = new List<FrameData>();
                for (int i = 0; i < frameCount; i++)
                {
                    // 100x100-as RGB képkocka (üres byte tömb elég)
                    frames.Add(new FrameData
                    {
                        FrameIndex = i,
                        Width = 100,
                        Height = 100,
                        PixelData = new byte[100 * 100 * 3]
                    });
                }

                // Feldolgozás futtatása a Work Pool modellel
                var results = WorkPoolProcessor.Process(frames, pipeline);

                // Lokális referenciák explicit felszabadítása,
                // hogy a GC a következő futáskor takaríthasson
                frames.Clear();
                results = null;
            };

            // ===== 1. BEMELEGÍTÉS (WARMUP) =====
            // Kis kötegen futtatjuk a feldolgozást, hogy a .NET runtime
            // lefoglalja a Thread stacket, a JIT lefordítsa a metódusokat,
            // és a ConcurrentBag/TLS belső pufferei előmelegedjenek.
            // Ezek az allokációk NEM szivárgások, csak indulási overhead!
            processBatch(10);

            // Kényszerített, agresszív takarítás a bemelegítés után:
            //   - LOH (Large Object Heap) tömörítése (a 30 KB feletti
            //     tömbök ide kerülnek, és normál GC-vel nem tömörülnek)
            //   - Teljes Gen2 GC futtatása (minden generáció)
            //   - Megvárjuk a finalizálók lefutását
            //   - Egy újabb GC, hogy a finalizált objektumok is eltűnjenek
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // ---- Alaphelyzet mérése a bemelegítés UTÁN ----
            // A 'true' paraméter teljes GC futtatást kényszerít a mérés előtt
            long memoryBefore = GC.GetTotalMemory(true);

            // ---- Act ----
            // ===== 2. VALÓDI TESZT =====
            // 50 képkocka feldolgozása — ez ~184 MB allokációval jár
            // normál működés mellett. Ha az implementáció szivárog,
            // ennek nagy része "ottmarad" a heapen a takarítás után is.
            processBatch(50);

            // Ugyanaz az agresszív takarítás, mint a bemelegítés után —
            // most viszont KELL hogy minden átmeneti allokáció eltűnjön
            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // ---- Assert ----
            // Memória mérése a takarítás után
            long memoryAfter = GC.GetTotalMemory(true);

            // Tűréshatár: 50 MB
            //   - A .NET CLR belső pooljai (ArrayPool stb.) a
            //     teljesítmény érdekében visszatartanak puffereket
            //   - Az MSTest framework önmagában is fogyaszt memóriát
            //     a tesztfuttatás során
            //   - Egy VALÓDI szivárgás 50 képkockánál ~184 MB-ot
            //     hagyna a memóriában → 50 MB küszöb messze elég
            //     biztonságos a hamis riasztások elkerülésére
            long diffMB = (memoryAfter - memoryBefore) / (1024 * 1024);

            // A különbségnek kisebb-egyenlőnek kell lennie 50 MB-nál.
            // A hibaüzenet részletes: ha bukik a teszt, látható
            // mind a különbség, mind a két abszolút érték
            Assert.IsLessThanOrEqualTo(50, diffMB,
                $"Memóriaszivárgás észlelve! {diffMB} MB maradt a memóriában. " +
                $"(Before: {memoryBefore / 1024 / 1024}MB, " +
                $"After: {memoryAfter / 1024 / 1024}MB)");
        }
    }
}