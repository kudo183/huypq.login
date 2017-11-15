$(function () {
    window.params = (function () {
        var result = {};
        window.location.search.substring(1).split("&").forEach(function (p) {
            var keyValue = p.split("=");
            result[keyValue[0]] = decodeURIComponent(keyValue[1]);
        });
        return result;
    })();

    var token = window.localStorage.getItem("logintoken");

    if (token !== null) {
        $("#login-div").hide();
        $("#logout-div").show();
        $('#loginEmail').text(window.localStorage.getItem("email"));
    }

    $('#login-form-link').click(function (e) {
        $("#login-form").delay(100).fadeIn(100);
        $("#register-form").hide();
        $('#register-form-link').removeClass('active');
        $("#resetPassword-form").hide();
        $('#resetPassword-form-link').removeClass('active');
        $(this).addClass('active');
        e.preventDefault();
    });
    $('#register-form-link').click(function (e) {
        $("#register-form").delay(100).fadeIn(100);
        $("#login-form").hide();
        $('#login-form-link').removeClass('active');
        $("#resetPassword-form").hide();
        $('#resetPassword-form-link').removeClass('active');
        $(this).addClass('active');
        e.preventDefault();
    });
    $('#resetPassword-form-link').click(function (e) {
        $("#resetPassword-form").delay(100).fadeIn(100);
        $("#login-form").hide();
        $('#login-form-link').removeClass('active');
        $("#register-form").hide();
        $('#register-form-link').removeClass('active');
        $(this).addClass('active');
        e.preventDefault();
    });

    $("#login-form").submit(function (e) {

        $.ajax({
            type: "POST",
            url: "login",
            data: "email=" + $("#email").val() + "&password=" + $("#password").val()
        }).done(function (data) {
            var loginJson = JSON.parse(data);
            window.localStorage.setItem("logintoken", loginJson.token);
            window.localStorage.setItem("email", loginJson.email);

            if (window.params.redirect_uri !== undefined) {
                window.location.href = "authorize.html" + window.location.search;
            } else {
                $('#loginEmail').text(loginJson.email);
                $("#login-div").hide();
                $("#logout-div").show();
            }
        }).fail(function (jqXHR) {
            alert("fail: " + jqXHR.responseText);
        });

        e.preventDefault();
    });

    $("#register-form").submit(function (e) {

        $.ajax({
            type: "POST",
            url: "register",
            data: $("#register-form").serialize()
        }).done(function (data) {
            alert("done: " + data);
        }).fail(function (jqXHR) {
            alert("fail: " + jqXHR.responseText);
        });

        e.preventDefault();
    });

    $("#resetPassword-form").submit(function (e) {

        $.ajax({
            type: "POST",
            url: "requestresetpass",
            data: $("#resetPassword-form").serialize()
        }).done(function (data) {
            alert("done: " + data);
        }).fail(function (jqXHR) {
            alert("fail: " + jqXHR.responseText);
        });

        e.preventDefault();
    });

    $('#btn-logout').click(function (e) {
        window.localStorage.removeItem("logintoken");

        if (window.params.redirect_uri !== undefined) {
            window.location.href = window.params.redirect_uri;
        } else {
            $('#loginEmail').text("");
            $("#login-div").show();
            $("#logout-div").hide();
        }

        e.preventDefault();
    });

    $('#btn-logoutAll').click(function (e) {
        var token = window.localStorage.getItem("logintoken");
        $.ajax({
            type: "POST",
            url: "logout",
            data: "logintoken=" + token
        }).done(function (data) {
            window.localStorage.removeItem("logintoken");
            if (window.params.redirect_uri !== undefined) {
                window.location.href = window.params.redirect_uri;
            } else {
                $('#loginEmail').text("");
                $("#login-div").show();
                $("#logout-div").hide();
            }
        }).fail(function (jqXHR) {
            alert("fail: " + jqXHR.responseText);
        });
        e.preventDefault();
    });
});