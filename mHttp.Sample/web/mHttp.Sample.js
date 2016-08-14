var app = angular.module('mHttpSample', ['ngResource']);

app.factory('MetricsService', ['$resource', function($resource) {
    var hoursToGraph = 1 * 24;
    var processMetricsResponse = function(response) {
        var metrics = response.data;

        var sessionsErrors = metrics.Backend.Sessions.Errors;
        metrics.Backend.Sessions.$totalErrors = _.reduce(_.values(sessionsErrors), function(z, count) { return z + count; }, 0);

        _.each(metrics.HostReports[0].Endpoints, function(endpoint) {
            endpoint.$endpointId = endpoint.Method + ':' + endpoint.Route;

            endpoint.$responsesByStatusCode = _.reduce(endpoint.StatusCodeCountersByHour, function(z, entryForHour) {
                if (entryForHour.TimeHours > metrics.TimeHours - hoursToGraph) {
                    _.each(entryForHour.StatusCodeCounters, function(countEntry) {
                        z[countEntry.StatusCode] = z[countEntry.StatusCode] ? z[countEntry.StatusCode] + countEntry.Count : countEntry.Count;
                    });
                };
                return z;
            }, {});

            endpoint.$responses = _.reduce(endpoint.StatusCodeCountersByHour, function(z, entryForHour) {
                if (entryForHour.TimeHours > metrics.TimeHours - hoursToGraph) {
                    return z + _.reduce(entryForHour.StatusCodeCounters, function(z2, countEntry) {
                        return z2 + countEntry.Count;
                    }, 0);
                } else {
                    return z;
                }
            }, 0);

            endpoint.$totalResponses = _.reduce(endpoint.StatusCodeCounters, function(z, counter) { return z + counter.Count; }, 0);

            var handlerTimesByPercentileAsc = _.sortBy(endpoint.HandlerTimes, function(entry) { return entry.Percentile; });
            endpoint.$handlerTime = _.map(handlerTimesByPercentileAsc, function(entry) { return entry.Value; });
        });

        return metrics;
    };

    return $resource('/metrics', {}, {
        get: {method:'GET', params: {}, isArray: false, interceptor: { response: processMetricsResponse }}
    });
}]);

app.controller('MetricsController', ['$rootScope', '$scope', 'MetricsService', function($rootScope, $scope, MetricsService) {
    $scope.refreshing = false;
    var showResponseDetails = {};

    var refreshMetrics = function() {
        $scope.refreshing = true;

        MetricsService.get().$promise.then(
            function(result) {
                _.each(result.HostReports[0].Endpoints, function(endpoint) {
                    var endpointId = endpoint.$endpointId;
                    endpoint.$showResponseDetails = showResponseDetails[endpointId];
                    endpoint.$toggleResponseDetails = function() {
                        showResponseDetails[endpointId] = !showResponseDetails[endpointId];
                        endpoint.$showResponseDetails = showResponseDetails[endpointId];
                    };
                    endpoint.$showResponsesGraph = function() {
                        $rootScope.$broadcast('showResponsesGraph', { timeHours: result.TimeHours, endpoint: endpoint });
                    };
                });

                result.Backend.Sessions.$showTotalErrors = function() {
                    $rootScope.$broadcast('showTotalErrors', result.Backend.Sessions.Errors);
                };

                $scope.metrics = result;
            },
            function() {
                alert('Error refreshing metrics');
            }
        ).finally(function() { $scope.refreshing = false; });
    };
    $scope.refreshMetrics = refreshMetrics;

    $rootScope.$on('refreshMetrics', refreshMetrics);
}]);

app.controller('SessionsErrorsController', ['$rootScope', '$scope', function($rootScope, $scope) {
    $scope.errors = {};
    $rootScope.$on('showTotalErrors', function(evt, args) {
        $scope.errors = args;
        $('#sessionsErrorsModal').modal('show');
    });
}]);

app.controller('ResponsesGraphController', ['$rootScope', '$scope', function($rootScope, $scope) {
    var getDataPoints = function(endpoint, statusCodeToGraph, nowEpochHour, hoursToGraph) {
        var data = [];
        for (var i=0; i<hoursToGraph; i++) {
            var hour = nowEpochHour - i;
            var hourDate = new Date(hour * 60 * 60 * 1000);
            var countersForHour = _.find(endpoint.StatusCodeCountersByHour, function(entry) { return entry.TimeHours == hour; });

            if (countersForHour) {
                var counterForCode = _.find(countersForHour.StatusCodeCounters, function(entry) { return entry.StatusCode == statusCodeToGraph; });
                var count = counterForCode ? counterForCode.Count : 0;
                data.push({ date: hourDate, value: count });
            } else {
                data.push({ date: hourDate, value: 0 });
            }
        }

        return data;
    };

    var drawResponsesGraph = function(target, nowEpochHour, endpoint, statusCodeToGraph, hoursToGraph) {
        var data = getDataPoints(endpoint, statusCodeToGraph, nowEpochHour, hoursToGraph);
        var total = _.reduce(data, function(z, entry) { return z + entry.value; }, 0);

        MG.data_graphic({
            title: 'HTTP ' + statusCodeToGraph + ' responses (' + hoursToGraph + ' hrs)',
            data: data,
            target: target,
            width: 600,
            height: 250,
            color: '#8C001A',
            show_rollover_text: true,
            show_secondary_x_label: false,
            x_mouseover: function(point) { return moment(point.date).utc().format('MMM DD ddd HH:mm') + " UTC - "; },
            utc_time: true,
            interpolate: 'monotone'
        });
    };

    $scope.showGraph = function(statusCode) {
        drawResponsesGraph('#responsesGraph', $scope.timeHours, $scope.endpoint, statusCode, 1 * 24);
    };

    $rootScope.$on('showResponsesGraph', function(evt, args) {
        $scope.timeHours = args.timeHours;
        $scope.endpoint = args.endpoint;

        var availableResponseCodes = _.keys($scope.endpoint.$responsesByStatusCode);
        if (availableResponseCodes.length > 0) {
            $scope.showGraph(availableResponseCodes[0]);
        }

        $('#responsesGraphModal').modal('show');
    });
}]);

app.controller('WebSocketController', ['$rootScope', '$scope', function($rootScope, $scope) {
    $scope.connected = false;

    var displayMessage = function(msg) {
        msg = _.escape(msg);
        var messages = $('#messages');
        messages.append('[' + moment().format('HH:mm:ss') + '] ' + msg + '\r\n');
        messages.animate({scrollTop: messages[0].scrollHeight}, 250);
    };

    $scope.connect = function() {
        var ws = new WebSocket('ws://' + location.host + '/ws');

        ws.onopen = function(evt) {
            $rootScope.$broadcast('refreshMetrics');
            $scope.connected = true;
            $scope.$apply();
        };

        ws.onclose = function(evt) {
            $rootScope.$broadcast('refreshMetrics');
            $scope.connected = false;
            displayMessage('*** Disconnected');
            $scope.$apply();
        };

        ws.onmessage = function(evt) {
            displayMessage(evt.data);
            $scope.$apply();
        };

        $scope.ws = ws;
    };

    $scope.disconnect = function() {
        $scope.ws.close();
    };

    $scope.sendMessage = function() {
        if ($scope.connected && $scope.message) {
            $scope.ws.send($scope.message);
            $scope.message = null;
        }
    };

    $('#input').focus();
}]);
