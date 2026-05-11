using AwesomeAssertions;
using Codify.GrpcCodeFirstDataAnnotations.Internal;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Codify.GrpcCodeFirstDataAnnotations.Test.Unit;

public class RequiredPropertiesValidatorTest
{
    // -------------------------------------------------------------------------
    // Required-only scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_ReturnNoFailures_When_AllRequiredPropertiesAreSet()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredOnly>();

        var results = validator.Validate(new ModelWithRequiredOnly { Name = "Alice", Description = "Desc" });

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_ReturnFailure_When_RequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredOnly>();

        // Force a null into a required non-nullable property via unsafe cast
        var instance = (ModelWithRequiredOnly)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithRequiredOnly));

        var results = validator.Validate(instance);

        results.Should().HaveCount(2);
        results.Should().ContainSingle(r => r.ErrorMessage == "'Name' must not be null");
        results.Should().ContainSingle(r => r.ErrorMessage == "'Description' must not be null");
    }

    [Fact]
    public void Validate_Should_ReturnOneFailure_When_OneRequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredOnly>();

        var instance = (ModelWithRequiredOnly)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithRequiredOnly));

        // Set only Name; Description remains null
        typeof(ModelWithRequiredOnly)
            .GetProperty(nameof(ModelWithRequiredOnly.Name))!
            .SetValue(instance, "Alice");

        var results = validator.Validate(instance);

        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("'Description' must not be null");
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_For_ModelWithNoRequiredMembers()
    {
        // ModelWithDataAnnotationsOnly has no C# required keyword, only [Required] attributes
        var validator = new RequiredPropertiesValidator<ModelWithDataAnnotationsOnly>();

        var results = validator.Validate(new ModelWithDataAnnotationsOnly { Name = null, Code = null });

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_IgnoreNullableRequiredProperty()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNullableRequired>();

        var instance = (ModelWithNullableRequired)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNullableRequired));

        var results = validator.Validate(instance);

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Combined scenarios (required keyword + data annotations together)
    // -------------------------------------------------------------------------

    [Fact]
    public void Combined_Should_DetectBothAnnotationAndRequiredFailures_When_BothPropertiesNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithBoth>();

        var instance = (ModelWithBoth)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithBoth));

        var validationContext = new ValidationContext(instance);
        var annotationResults = new System.Collections.Generic.List<ValidationResult>();
        Validator.TryValidateObject(instance, validationContext, annotationResults, validateAllProperties: true);

        var requiredResults = validator.Validate(instance);

        // [Required] catches Name via data annotations
        annotationResults.Should().ContainSingle(r => r.MemberNames.Contains(nameof(ModelWithBoth.Name)));

        // RequiredPropertiesValidator catches Code via required keyword
        requiredResults.Should().ContainSingle(r => r.ErrorMessage == "'Code' must not be null");

        // Combined list covers both failures
        var combined = new System.Collections.Generic.List<ValidationResult>(annotationResults);
        combined.AddRange(requiredResults);
        combined.Should().HaveCount(2);
    }

    [Fact]
    public void Combined_Should_ReturnNoFailures_When_AllPropertiesSet()
    {
        var model = new ModelWithBoth { Name = "Alice", Code = "greet" };
        var validator = new RequiredPropertiesValidator<ModelWithBoth>();
        var validationContext = new ValidationContext(model);
        var annotationResults = new System.Collections.Generic.List<ValidationResult>();
        Validator.TryValidateObject(model, validationContext, annotationResults, validateAllProperties: true);

        var requiredResults = validator.Validate(model);

        annotationResults.Should().BeEmpty();
        requiredResults.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Nested required properties
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_ReturnNestedFailure_When_NestedRequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>();

        // Child is set but its inner Value is null
        var inner = (ModelWithNestedRequired.Inner)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired.Inner));
        var instance = new ModelWithNestedRequired { Child = inner };

        var results = validator.Validate(instance);

        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("'Child.Value' must not be null");
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_When_AllNestedPropertiesAreSet()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>();

        var results = validator.Validate(new ModelWithNestedRequired
        {
            Child = new ModelWithNestedRequired.Inner { Value = "hello" }
        });

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // ValidationResult MemberNames
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_PopulateMemberNames_When_RequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredOnly>();

        var instance = (ModelWithRequiredOnly)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithRequiredOnly));

        var results = validator.Validate(instance);

        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(ModelWithRequiredOnly.Name)));
        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(ModelWithRequiredOnly.Description)));
    }

    [Fact]
    public void Validate_Should_PopulateNestedMemberName_When_NestedRequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>();

        var inner = (ModelWithNestedRequired.Inner)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired.Inner));
        var instance = new ModelWithNestedRequired { Child = inner };

        var results = validator.Validate(instance);

        results.Should().ContainSingle()
            .Which.MemberNames.Should().Contain("Child.Value");
    }

    // -------------------------------------------------------------------------
    // Null parent suppresses child errors
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_NotReportChildErrors_When_ParentIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>();

        // Child (Parent) itself is null — the nested Value rule should not fire separately
        var instance = (ModelWithNestedRequired)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired));

        var results = validator.Validate(instance);

        // Only the top-level Child failure; no separate Child.Value failure
        results.Should().ContainSingle(r => r.ErrorMessage == "'Child' must not be null");
        results.Should().NotContain(r => r.ErrorMessage == "'Child.Value' must not be null");
    }

    // -------------------------------------------------------------------------
    // Array properties are skipped (not recursed into)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_CheckArrayPropertyForNull_But_NotRecurseIntoIt()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredArray>();

        var instance = (ModelWithRequiredArray)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithRequiredArray));

        var results = validator.Validate(instance);

        // Both Tags (array) and Name (string) are required non-nullable — both null → two failures
        results.Should().HaveCount(2);
        results.Should().ContainSingle(r => r.ErrorMessage == "'Tags' must not be null");
        results.Should().ContainSingle(r => r.ErrorMessage == "'Name' must not be null");
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_When_ArrayPropertyIsSet()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredArray>();

        var results = validator.Validate(new ModelWithRequiredArray { Tags = ["a", "b"], Name = "Alice" });

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Generic collection properties (List<T>) — not arrays, no recursion expected
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_CheckListPropertyForNull_But_NotRecurseIntoIt()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredList>();

        var instance = (ModelWithRequiredList)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithRequiredList));

        var results = validator.Validate(instance);

        results.Should().HaveCount(2);
        results.Should().ContainSingle(r => r.ErrorMessage == "'Items' must not be null");
        results.Should().ContainSingle(r => r.ErrorMessage == "'Name' must not be null");
    }

    // -------------------------------------------------------------------------
    // Deeply nested (3+ levels)
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_ReturnDeepNestedFailure_When_DeepPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithDeeplyNested>();

        var level2 = (ModelWithDeeplyNested.Level1.Level2)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithDeeplyNested.Level1.Level2));
        var level1 = new ModelWithDeeplyNested.Level1 { B = level2 };
        var instance = new ModelWithDeeplyNested { A = level1 };

        var results = validator.Validate(instance);

        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("'A.B.Value' must not be null");
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_When_DeepNestedPropertiesAreSet()
    {
        var validator = new RequiredPropertiesValidator<ModelWithDeeplyNested>();

        var results = validator.Validate(new ModelWithDeeplyNested
        {
            A = new ModelWithDeeplyNested.Level1
            {
                B = new ModelWithDeeplyNested.Level1.Level2 { Value = "deep" }
            }
        });

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Non-required parent whose child type has required members
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_NotFlagChildProperties_When_ParentPropertyIsNotRequired()
    {
        // The Optional property itself is nullable, so RequiredPropertiesValidator
        // should not register or fire rules for its children
        var validator = new RequiredPropertiesValidator<ModelWithNonRequiredParentHavingRequiredChildren>();

        var instance = new ModelWithNonRequiredParentHavingRequiredChildren { Optional = null };

        var results = validator.Validate(instance);

        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Max depth
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_Should_NotValidateNestedProperties_When_MaxDepthIsZero()
    {
        // depth 0 means only top-level properties of T itself are checked
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>(maxDepth: 0);

        var inner = (ModelWithNestedRequired.Inner)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired.Inner));
        var instance = new ModelWithNestedRequired { Child = inner };

        var results = validator.Validate(instance);

        // Child is set so no top-level failure; Child.Value is beyond depth 0 so not checked
        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_Should_StillValidateTopLevel_When_MaxDepthIsZero()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>(maxDepth: 0);

        var instance = (ModelWithNestedRequired)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired));

        var results = validator.Validate(instance);

        // Top-level Child is null and depth 0 still covers level 1 properties of T
        results.Should().ContainSingle(r => r.ErrorMessage == "'Child' must not be null");
    }

    [Fact]
    public void Validate_Should_ValidateOneLevel_When_MaxDepthIsOne()
    {
        var validator = new RequiredPropertiesValidator<ModelWithDeeplyNested>(maxDepth: 1);

        var level1 = (ModelWithDeeplyNested.Level1)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithDeeplyNested.Level1));
        var instance = new ModelWithDeeplyNested { A = level1 };

        var results = validator.Validate(instance);

        // A is set; A.B is null and is at depth 1 — should be reported
        results.Should().ContainSingle(r => r.ErrorMessage == "'A.B' must not be null");
    }

    [Fact]
    public void Validate_Should_NotValidateBeyondMaxDepth_When_LimitReached()
    {
        // depth 1: A.B is checked but A.B.Value (depth 2) is not
        var validator = new RequiredPropertiesValidator<ModelWithDeeplyNested>(maxDepth: 1);

        var level2 = (ModelWithDeeplyNested.Level1.Level2)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithDeeplyNested.Level1.Level2));
        var level1 = new ModelWithDeeplyNested.Level1 { B = level2 };
        var instance = new ModelWithDeeplyNested { A = level1 };

        var results = validator.Validate(instance);

        // A.B is set so no depth-1 failure; A.B.Value is beyond maxDepth so not checked
        results.Should().BeEmpty();
    }

    #region Test models

    private class ModelWithRequiredOnly
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
    }

    private class ModelWithDataAnnotationsOnly
    {
        [Required]
        public string? Name { get; init; }

        [MinLength(4)]
        public string? Code { get; init; }
    }

    private class ModelWithBoth
    {
        // Covered by [Required] data annotation
        [Required]
        public string? Name { get; init; }

        // Covered only by the required keyword (non-nullable reference)
        public required string Code { get; init; }
    }

    private class ModelWithNestedRequired
    {
        public required Inner Child { get; init; }

        public class Inner
        {
            public required string Value { get; init; }
        }
    }

    private class ModelWithNullableRequired
    {
        // Nullable required — should NOT be flagged by RequiredPropertiesValidator
        public required string? Name { get; init; }
    }

    private class ModelWithRequiredArray
    {
        public required string[] Tags { get; init; }
        public required string Name { get; init; }
    }

    private class ModelWithRequiredList
    {
        public required List<string> Items { get; init; }
        public required string Name { get; init; }
    }

    private class ModelWithDeeplyNested
    {
        public required Level1 A { get; init; }

        public class Level1
        {
            public required Level2 B { get; init; }

            public class Level2
            {
                public required string Value { get; init; }
            }
        }
    }

    private class ModelWithNonRequiredParentHavingRequiredChildren
    {
        // Parent is not required — validator should not recurse into it unless it has [RequiredMemberAttribute]
        public Child? Optional { get; init; }

        public class Child
        {
            public required string Value { get; init; }
        }
    }

    #endregion
}
