namespace m.Http.Backend.Tcp
{
    enum RequestState
    {
        ReadRequestLine,
        ReadHeaders,
        ReadBody,
        Completed
    }
}