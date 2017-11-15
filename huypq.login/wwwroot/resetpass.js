$(function () {
    window.params = (function () {
        var result = {};
        window.location.search.substring(1).split("&").forEach(function (p) {
            var keyValue = p.split("=");
            result[keyValue[0]] = decodeURIComponent(keyValue[1]);
        });
        return result;
    })();
    
    $("#resetPassword-form").submit(function (e) {

        $.ajax({
            type: "POST",
            url: "resetpass",
            data: "token=" + window.params.token + "&password=" + $("#password").val()
        }).done(function (data) {
            alert("done: " + data);
        }).fail(function (jqXHR) {
            alert("fail: " + jqXHR.responseText);
        });

        e.preventDefault();
    });
});