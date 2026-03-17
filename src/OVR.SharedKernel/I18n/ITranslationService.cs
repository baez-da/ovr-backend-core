namespace OVR.SharedKernel.I18n;

public interface ITranslationService
{
    string Translate(string errorCode, string language, string fallback,
        IDictionary<string, object>? parameters = null);

    string TranslateFieldName(string propertyName, string language);
}
