using System.Runtime.ExceptionServices;

namespace Velvet.Async;

internal class ExceptionHolder
{
    private ExceptionDispatchInfo exception;
    private bool                  calledGet = false;

    public ExceptionHolder(ExceptionDispatchInfo exception)
    {
        this.exception = exception;
    }

    public ExceptionDispatchInfo GetException()
    {
        if (!calledGet)
        {
            calledGet = true;
            GC.SuppressFinalize(this);
        }
        return exception;
    }

    ~ExceptionHolder()
    {
        if (!calledGet)
        {
            VelvetTaskScheduler.PublishUnobservedTaskException(exception.SourceException);
        }
    }
}