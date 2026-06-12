namespace NoteMark
{
    using System;

    public sealed class RenderResult
    {
        public bool IsSuccess { get; set; }
        public byte[] ImageData { get; set; }
        public string TextData { get; set; }
        public double RenderTimeMs { get; set; }
        public string ErrorMessage { get; set; }

        public RenderResult()
        {
            IsSuccess = false;
            ImageData = null;
            TextData = string.Empty;
            RenderTimeMs = 0.0;
            ErrorMessage = string.Empty;
        }

        public static RenderResult Success(byte[] imageData, double renderTimeMs)
        {
            return new RenderResult
            {
                IsSuccess = true,
                ImageData = imageData,
                RenderTimeMs = renderTimeMs,
                TextData = string.Empty,
                ErrorMessage = string.Empty
            };
        }

        public static RenderResult Failure(string errorMessage)
        {
            return new RenderResult
            {
                IsSuccess = false,
                ImageData = null,
                RenderTimeMs = 0.0,
                TextData = string.Empty,
                ErrorMessage = errorMessage ?? string.Empty
            };
        }
    }
}
