var app = angular.module('mHttpSample', ['ngResource']);

app.factory('MetricsService', ['$resource',
    function($resource) {
        return $resource('/metrics', {}, {
        get: {method:'GET', params: {}, isArray: false}
    });
  }
]);

app.controller('MetricsController', ['$scope', 'MetricsService', function($scope, MetricsService) {
    $scope.connected = false;

    var showResponseDetails = {};

    var refreshMetrics = function() {
        MetricsService.get().$promise.then(function(result) {
            _.each(result.HostReports[0].Endpoints, function(endpoint) {
                var endpointId = endpoint.Method + ':' + endpoint.Route;
                endpoint.$showResponseDetails = showResponseDetails[endpointId];
                endpoint.$totalResponses = _.reduce(endpoint.StatusCodeCounters, function(z, counter) { return z + counter.Count; }, 0);
                var handlerTimesByPercentileAsc = _.sortBy(endpoint.HandlerTimes, function(entry) { return entry.Percentile; });
                endpoint.$handlerTime = _.map(handlerTimesByPercentileAsc, function(entry) { return entry.Value; });

                endpoint.toggleResponseDetails = function() {
                    showResponseDetails[endpointId] = !showResponseDetails[endpointId];
                    endpoint.$showResponseDetails = showResponseDetails[endpointId];
                }
            });

            $scope.metrics = result;
        });
    };

    setInterval(refreshMetrics, 1000);

    var displayMessage = function(msg) {
        msg = _.escape(msg);
        var messages = $('#messages');
        messages.append('[' + moment().format('HH:mm:ss') + '] ' + msg + '\r\n');
        messages.animate({scrollTop: messages[0].scrollHeight}, 250);
    };

    $scope.connect = function() {
        var ws = new WebSocket('ws://' + location.host + '/ws');

        ws.onopen = function(evt) {
            $scope.connected = true;
        };

        ws.onclose = function(evt) {
            $scope.connected = false;
            displayMessage('*** Disconnected');
        };

        ws.onmessage = function(evt) {
            displayMessage(evt.data);
        };

      $scope.ws = ws;
    };

    $scope.disconnect = function() {
        $scope.ws.close();
    };

    $scope.sendMessage = function() {
        if ($scope.connected) {
            if (!$scope.message) {
                return;
            } else {
                $scope.ws.send($scope.message);
                $scope.message = null;
            }
        } else {
        }
    };

    $('#input').focus();

    $scope.connect();
}]);
