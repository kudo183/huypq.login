$(function () {
    window.params = (function () {
        var result = {};
        window.location.search.substring(1).split("&").forEach(function (p) {
            var keyValue = p.split("=");
            result[keyValue[0]] = decodeURIComponent(keyValue[1]);
        });
        return result;
    })();
    
    var logintoken = window.localStorage.getItem("logintoken");
    if (logintoken === null) {
        window.location.href = "index.html" + window.location.search;
    } else {
        var postData = "logintoken=" + logintoken + "&redirect_uri=" + window.params.redirect_uri;
        if (window.params.state !== undefined) {
            postData = postData + "&state=" + window.params.state;
        }
        if (window.params.nonce !== undefined) {
            postData = postData + "&nonce=" + window.params.nonce;
        }
        if (window.params.code_challenge !== undefined) {
            postData = postData + "&code_challenge=" + window.params.code_challenge;
        }
        if (window.params.code_challenge_method !== undefined) {
            postData = postData + "&code_challenge_method=" + window.params.code_challenge_method;
        }
        if (window.params.response_type !== undefined) {
            postData = postData + "&response_type=" + window.params.response_type;
        }
        if (window.params.scope !== undefined) {
            postData = postData + "&scope=" + window.params.scope;
        }
        if (window.params.client_id !== undefined) {
            postData = postData + "&client_id=" + window.params.client_id;
        }
        $.ajax({
            type: "POST",
            url: "authorizecode",
            data: postData
        }).done(function (response) {

            var newUrl = window.params.redirect_uri;
            var json = JSON.parse(response);
            newUrl = newUrl + "?code=" + json.code;
            if (json.state !== "") {
                newUrl = newUrl + "&state=" + json.state;
            }
            window.location.href = newUrl;
        }).fail(function (jqXHR) {
            alert("fail: " + jqXHR.responseText);
        });
    }
});