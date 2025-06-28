namespace XRCultureWebApp.Pages
{
    public class HTTPResponse
    {
        public static readonly string Success =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>200</Status></ViewModelResponse>";
        public static readonly string SuccessWithParameters =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>200</Status><Parameters>%PARAMETERS%</Parameters></ViewModelResponse>";
        public static readonly string BadRequest =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>400</Status><Message>%MESSAGE%</Message></ViewModelResponse>";
        public static readonly string ServerError =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>500</Status><Message>%MESSAGE%</Message></ViewModelResponse>";
        public static readonly string NotFound =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ViewModelResponse><Status>404</Status><Message>%MESSAGE%</Message></ViewModelResponse>";
    }
}
