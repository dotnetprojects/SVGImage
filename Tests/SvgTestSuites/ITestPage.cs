using System;

namespace SvgTestSuites
{
    public interface ITestPage
    {
        bool LoadDocument(string documentFilePath, SvgTestInfo testInfo, object extraInfo);
        void UnloadDocument();
    }
}
