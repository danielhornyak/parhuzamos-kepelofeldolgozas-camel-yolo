using ParallelPreprocessing.Models;
using ParallelPreprocessing.Preprocessing;
using ParallelPreprocessing.Processing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

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
        [TestMethod]
        public void CropStep_30Percent_CalculatesCorrectDimensions()
        {
            int width = 100;
            int height = 100;
            byte[] dummyPixels = new byte[width * height * 3]; // RGB → 3 byte/pixel

            Array.Fill(dummyPixels, (byte)128);

            var inputFrame = new FrameData
            {
                Width = width,
                Height = height,
                PixelData = dummyPixels
            };

            var cropStep = new CropStep(0.30f);

            var resultFrame = cropStep.Process(inputFrame);

            int expectedHeight = (int)(height * (1 - 0.30f));

            Assert.IsNotNull(resultFrame, "A vágás eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.PixelData, "A pixel adatnak léteznie kell.");

            Assert.AreEqual(width, resultFrame.Width,
                "A szélességnek változatlannak kell maradnia.");

            Assert.AreEqual(expectedHeight, resultFrame.Height,
                "A magasságnak pontosan 30%-kal kell csökkennie.");

            Assert.HasCount(width * expectedHeight * 3, resultFrame.PixelData,
                "A pixel tömb mérete hibás.");
        }

        // ============================================================
        // TESZT 2: ResizeStep — bilineáris interpoláció helyessége
        // ============================================================
        [TestMethod]
        public void ResizeStep_BilinearInterpolation_ScalesCorrectly()
        {
            int srcWidth = 4;
            int srcHeight = 4;
            int targetWidth = 2;
            int targetHeight = 2;

            byte[] pixels = new byte[srcWidth * srcHeight * 3];
            for (int i = 0; i < pixels.Length; i += 3)
            {
                pixels[i] = 0;       // B csatorna (0. index) — nincs kék
                pixels[i + 1] = 0;   // G csatorna (1. index) — nincs zöld
                pixels[i + 2] = 255; // R csatorna (2. index) — maximális piros
            }

            var inputFrame = new FrameData
            {
                Width = srcWidth,
                Height = srcHeight,
                PixelData = pixels
            };

            var resizeStep = new ResizeStep(targetWidth, targetHeight);

            var resultFrame = resizeStep.Process(inputFrame);

            Assert.IsNotNull(resultFrame, "Az átméretezés eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.PixelData, "A pixel adatnak léteznie kell.");

            Assert.AreEqual(targetWidth, resultFrame.Width,
                "A célszélesség hibásan lett kiszámítva.");
            Assert.AreEqual(targetHeight, resultFrame.Height,
                "A célmagasság hibásan lett kiszámítva.");

            Assert.HasCount(targetWidth * targetHeight * 3, resultFrame.PixelData,
                "A kimeneti tömb hossza érvénytelen.");

            for (int i = 0; i < resultFrame.PixelData.Length; i += 3)
            {
                // B csatorna (0. index) közel nulla kell legyen
                Assert.IsLessThanOrEqualTo((byte)5, resultFrame.PixelData[i],
                    $"B csatorna interpolációs hiba a {i} indexnél.");

                // G csatorna (1. index) közel nulla kell legyen
                Assert.IsLessThanOrEqualTo((byte)5, resultFrame.PixelData[i + 1],
                    $"G csatorna interpolációs hiba a {i + 1} indexnél.");

                // R csatorna (2. index) közel maximális kell legyen
                Assert.IsGreaterThanOrEqualTo((byte)250, resultFrame.PixelData[i + 2],
                    $"R csatorna interpolációs hiba a {i + 2} indexnél.");
            }
        }

        // ============================================================
        // TESZT 3: NormalizeStep — byte → float konverzió + BGR→RGB swap
        // ============================================================
        [TestMethod]
        public void NormalizeStep_ConvertsByteToFloatCorrectly()
        {
            // 1×1 pixel BGR sorrendben: B=0, G=127, R=255
            byte[] pixels = new byte[] { 0, 127, 255 };
            var inputFrame = new FrameData
            {
                Width = 1,
                Height = 1,
                PixelData = pixels
            };
            var normalizeStep = new NormalizeStep();

            var resultFrame = normalizeStep.Process(inputFrame);

            Assert.IsNotNull(resultFrame, "A normalizálás eredménye nem lehet null.");
            Assert.IsNotNull(resultFrame.NormalizedData, "A normalizált adat nem lehet null.");
            Assert.HasCount(3, resultFrame.NormalizedData, "A normalizált tömb hossza nem egyezik a bemenettel.");

            // BGR→RGB swap után: [0]=R=1.0, [1]=G≈0.498, [2]=B=0.0
            Assert.AreEqual(1.0f, resultFrame.NormalizedData[0], 0.001f,
                "BGR→RGB: R csatorna (255 byte → 1.0f) hiba.");

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
        [TestMethod]
        public void Consistency_WorkPool_ProducesBitIdenticalResultToSequential()
        {
            int width = 320;
            int height = 240;

            var pipeline = new PreprocessingPipeline(640, 480, 0.3f);

            var frames = new List<FrameData>();
            var rnd = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                byte[] data = new byte[width * height * 3];
                rnd.NextBytes(data);
                frames.Add(new FrameData
                {
                    FrameIndex = i,
                    Width = width,
                    Height = height,
                    PixelData = data
                });
            }

            var sequentialResults = SequentialProcessor.Process(frames, pipeline);
            var workPoolResults = WorkPoolProcessor.Process(frames, pipeline);

            Assert.HasCount(sequentialResults.Length, workPoolResults,
                "A kimeneti darabszám eltér.");

            for (int i = 0; i < sequentialResults.Length; i++)
            {
                var seqFrame = sequentialResults[i];
                var poolFrame = workPoolResults.First(f => f.FrameIndex == i);

                Assert.IsNotNull(seqFrame.NormalizedData);
                Assert.IsNotNull(poolFrame.NormalizedData);

                for (int j = 0; j < seqFrame.NormalizedData.Length; j++)
                {
                    if (seqFrame.NormalizedData[j] != poolFrame.NormalizedData[j])
                    {
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
        [TestMethod]
        public void Memory_WorkPool_NoLeakAfterLargeBatch()
        {
            var pipeline = new PreprocessingPipeline(640, 480, 0.3f);

            Action<int> processBatch = (frameCount) =>
            {
                var frames = new List<FrameData>();
                for (int i = 0; i < frameCount; i++)
                {
                    frames.Add(new FrameData
                    {
                        FrameIndex = i,
                        Width = 100,
                        Height = 100,
                        PixelData = new byte[100 * 100 * 3]
                    });
                }

                var results = WorkPoolProcessor.Process(frames, pipeline);

                frames.Clear();
                results = null;
            };

            // 1. BEMELEGÍTÉS (WARMUP)
            processBatch(10);

            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryBefore = GC.GetTotalMemory(true);

            // 2. VALÓDI TESZT
            processBatch(50);

            System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long memoryAfter = GC.GetTotalMemory(true);

            long diffMB = (memoryAfter - memoryBefore) / (1024 * 1024);

            Assert.IsLessThanOrEqualTo(40,
diffMB, $"Memóriaszivárgás észlelve! {diffMB} MB maradt a memóriában. " +
                $"(Before: {memoryBefore / 1024 / 1024}MB, " +
                $"After: {memoryAfter / 1024 / 1024}MB)");
        }
    }
}