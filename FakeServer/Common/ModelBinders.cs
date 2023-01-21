using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FakeServer.Common;

// NOTE: Swagger requires FromRoute attribute when ModelBinder attribute is used or
// it will add the id-field as a parameter e.g. http://localhost:57602/api/users/{id}?id=1

public class DynamicBinder : ModelBinderAttribute
{
    public DynamicBinder()
    {
        BinderType = typeof(DynamicModelBinder);
    }
}

public class DynamicModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;

        var value = bindingContext.ValueProvider.GetValue(modelName);

        if (value == ValueProviderResult.None)
            return Task.CompletedTask;

        var stringValue = value.ToString();
        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, stringValue, stringValue);

        dynamic convertedId = ObjectHelper.GetIdAsCorrectType(stringValue);
        bindingContext.Result = ModelBindingResult.Success(convertedId);

        return Task.CompletedTask;
    }
}