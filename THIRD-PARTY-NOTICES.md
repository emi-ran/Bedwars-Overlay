# Third-party notices

## Tesseract OCR

Bedwars Overlay bundles the `TesseractOCR` .NET wrapper 5.5.2 and Tesseract 5.5.1 native engine under the Apache License 2.0. The English recognition data (`eng.traineddata`) is from `tessdata_fast` and is also distributed under Apache License 2.0. See [licenses/Apache-2.0.txt](licenses/Apache-2.0.txt).

- Wrapper: <https://github.com/Sicos1977/TesseractOCR>
- OCR engine: <https://github.com/tesseract-ocr/tesseract>
- English model: <https://github.com/tesseract-ocr/tessdata_fast>

## Microsoft Visual C++ runtime

Windows release packages include the x64 Visual C++ runtime DLLs needed by the bundled Tesseract native libraries. They are copied from the Microsoft Visual Studio redistributable files and distributed under the applicable Microsoft Visual Studio license terms: <https://learn.microsoft.com/cpp/windows/redistributing-visual-cpp-files>.
