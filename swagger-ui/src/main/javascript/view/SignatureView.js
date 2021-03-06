'use strict';

SwaggerUi.Views.SignatureView = Backbone.View.extend({
    events: {
        'click a.description-link': 'switchToDescription',
        'click a.snippet-link': 'switchToSnippet',
        'mousedown .snippet': 'snippetToTextArea'
    },

    initialize: function () {
    },

    render: function () {
        var self = this

        $(this.el).html(Handlebars.templates.signature(this.model));

        // $('.model-desc').html(this.model.signature)

        this.switchToSnippet();

        this.isParam = this.model.isParam;
        //console.log('model=', this.model)

        if (this.isParam) {
            $('.notice', $(this.el)).text('Click to set as parameter value');
            setTimeout(function () {
                // $('.snippet', $(self.el)).click()
                self.snippetToTextArea()
            }, 100)
        }

        return this;
    },

    // handler for show signature
    switchToDescription: function (e) {
        if (e) {
            e.preventDefault();
        }

        $('.snippet', $(this.el)).hide();
        $('.description', $(this.el)).show();
        $('.description-link', $(this.el)).addClass('selected');
        $('.snippet-link', $(this.el)).removeClass('selected');
    },

    // handler for show sample
    switchToSnippet: function (e) {
        if (e) {
            e.preventDefault();
        }

        $('.description', $(this.el)).hide();
        $('.snippet', $(this.el)).show();
        $('.snippet-link', $(this.el)).addClass('selected');
        $('.description-link', $(this.el)).removeClass('selected');
    },

    // handler for snippet to text area
    snippetToTextArea: function (e) {
        if (this.isParam) {
            if (e) {
                e.preventDefault();
            }

            var textArea = $('textarea', $(this.el.parentNode.parentNode.parentNode));

            // Fix for bug in IE 10/11 which causes placeholder text to be copied to "value"
            if ($.trim(textArea.val()) === '' || textArea.prop('placeholder') === textArea.val()) {
                textArea.val(this.model.sampleJSON);
            }
        }
    }
});