using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace WebApplication.Extensions
{
    public static class ModelStateExtensions
    {
        public static void AddModelError<TModel, TProperty>(
            this ModelStateDictionary modelState,
            Expression<Func<TModel, TProperty>> ex,
            string message
        )
        {
            var key = ExpressionHelper.GetExpressionText(ex);
            modelState.AddModelError(key, message);
        }
    }
}

