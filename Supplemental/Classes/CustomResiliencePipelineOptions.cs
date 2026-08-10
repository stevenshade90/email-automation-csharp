global using Polly;

using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Collections.Immutable;
using System.Net.Sockets;


namespace Email_Automation_Update.Supplemental.Classes_and_Engines
{
    internal class CustomResiliencePipelineOptions
    {
        public ResiliencePipeline ResiliencePipeline { get; set; }

        public CustomResiliencePipelineOptions()
        {
            ResiliencePipeline = new ResiliencePipelineBuilder()
                .AddRetry(RetryStrategy())
                .AddTimeout(TimeoutStrategy()) 
                .AddCircuitBreaker(CircuitBreakerStrategy())
                .Build();
        }

        public RetryStrategyOptions RetryStrategy()
        {
            ImmutableArray<Type> handledExceptions =
            [
                //Network exceptions
                typeof(SocketException),
                typeof(HttpRequestException),
                
                //Strategy exceptions
                typeof(TimeoutRejectedException),
                typeof(BrokenCircuitException)
            ];

            RetryStrategyOptions retryOptions = new RetryStrategyOptions()
            {
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Linear,
                Delay = TimeSpan.FromSeconds(3),
                MaxDelay = TimeSpan.FromSeconds(10),
                UseJitter = true,
                ShouldHandle = args =>
                {
                    var exceptionType = args.Outcome.Exception;

                    if (exceptionType == null)
                    {
                        return ValueTask.FromResult(false);
                    }

                    if (exceptionType is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
                    {
                        int code = (int)httpEx.StatusCode.Value;
                        if (code >= 400 && code < 500)
                        {
                            return ValueTask.FromResult(false);
                        }
                    }

                    bool isHandled = handledExceptions.Any(t => t.IsInstanceOfType(exceptionType));
                    return ValueTask.FromResult(isHandled);
                },
                OnRetry = static args =>
                {
                    Console.WriteLine($"\t\tAttempt Failed (Attempt: {args.AttemptNumber})");
                    return default;
                }
            };
            return retryOptions;
        }

        public TimeoutStrategyOptions TimeoutStrategy()
        {
            TimeoutStrategyOptions timeoutOptions = new TimeoutStrategyOptions()
            {
                Timeout = TimeSpan.FromSeconds(30),
                OnTimeout = args =>
                {

                    Console.WriteLine($"\t\tRequest timed out {(args.Context.OperationKey is not null ? $"({args.Context.OperationKey})" : default)}");
                    return default;
                }
            };
            return timeoutOptions;
        }

        public CircuitBreakerStrategyOptions CircuitBreakerStrategy()
        {
            CircuitBreakerStrategyOptions circuitBreakerOptions = new CircuitBreakerStrategyOptions()
            {
                FailureRatio = 0.5, 
                MinimumThroughput = 6,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(20),
                OnOpened = args =>
                {
                    Console.WriteLine($"\t\tCircuit breaker opened ({args.Context.OperationKey})");
                    return default;
                },
                OnClosed = args =>
                {
                    Console.WriteLine($"\t\tCircuit breaker closed ({args.Context.OperationKey})");
                    return default;
                },
            };
            return circuitBreakerOptions;
        }
    }
}