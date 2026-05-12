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

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
    }

    [Fact]
    public void Validate_Should_ReturnFailure_When_RequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredOnly>();

        // Force a null into a required non-nullable property via unsafe cast
        var instance = (ModelWithRequiredOnly)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithRequiredOnly));

        var results = validator.Validate(instance);

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Name must not be null.", MemberNames = new[] { "Name" } },
            new { ErrorMessage = "Description must not be null.", MemberNames = new[] { "Description" } }
        ]);
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Description must not be null.", MemberNames = new[] { "Description" } }
        ]);
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_For_ModelWithNoRequiredMembers()
    {
        // ModelWithDataAnnotationsOnly has no C# required keyword, only [Required] attributes
        var validator = new RequiredPropertiesValidator<ModelWithDataAnnotationsOnly>();

        var results = validator.Validate(new ModelWithDataAnnotationsOnly { Name = null, Code = null });

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
    }

    [Fact]
    public void Validate_Should_IgnoreNullableRequiredProperty()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNullableRequired>();

        var instance = (ModelWithNullableRequired)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNullableRequired));

        var results = validator.Validate(instance);

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
    }

    // -------------------------------------------------------------------------
    // Combined scenarios
    // -------------------------------------------------------------------------

    [Fact]
    public void Combined_Should_DetectBothAnnotationAndRequiredFailures_When_BothPropertiesNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithBoth>();

        var instance = (ModelWithBoth)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithBoth));

        var validationContext = new ValidationContext(instance);
        var annotationResults = new List<ValidationResult>();
        Validator.TryValidateObject(instance, validationContext, annotationResults, validateAllProperties: true);

        var requiredResults = validator.Validate(instance);

        // [Required] catches Name via data annotations
        annotationResults.Should().BeEquivalentTo([
            new { MemberNames = new[] { nameof(ModelWithBoth.Name) } }
        ], options => options.ExcludingMissingMembers());

        // RequiredPropertiesValidator catches Code via required keyword
        requiredResults.Should().BeEquivalentTo([
            new { ErrorMessage = "Code must not be null.", MemberNames = new[] { "Code" } }
        ]);
    }

    [Fact]
    public void Combined_Should_ReturnNoFailures_When_AllPropertiesSet()
    {
        var model = new ModelWithBoth { Name = "Alice", Code = "greet" };
        var validator = new RequiredPropertiesValidator<ModelWithBoth>();
        var validationContext = new ValidationContext(model);
        var annotationResults = new List<ValidationResult>();
        Validator.TryValidateObject(model, validationContext, annotationResults, validateAllProperties: true);

        var requiredResults = validator.Validate(model);

        annotationResults.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
        requiredResults.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Child.Value must not be null.", MemberNames = new[] { "Child.Value" } }
        ]);
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_When_AllNestedPropertiesAreSet()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>();

        var results = validator.Validate(new ModelWithNestedRequired
        {
            Child = new ModelWithNestedRequired.Inner { Value = "hello" }
        });

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Name must not be null.", MemberNames = new[] { "Name" } },
            new { ErrorMessage = "Description must not be null.", MemberNames = new[] { "Description" } }
        ]);
    }

    [Fact]
    public void Validate_Should_PopulateNestedMemberName_When_NestedRequiredPropertyIsNull()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>();

        var inner = (ModelWithNestedRequired.Inner)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired.Inner));
        var instance = new ModelWithNestedRequired { Child = inner };

        var results = validator.Validate(instance);

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Child.Value must not be null.", MemberNames = new[] { "Child.Value" } }
        ]);
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Child must not be null.", MemberNames = new[] { "Child" } }
        ]);
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Tags must not be null.", MemberNames = new[] { "Tags" } },
            new { ErrorMessage = "Name must not be null.", MemberNames = new[] { "Name" } }
        ]);
    }

    [Fact]
    public void Validate_Should_ReturnNoFailures_When_ArrayPropertyIsSet()
    {
        var validator = new RequiredPropertiesValidator<ModelWithRequiredArray>();

        var results = validator.Validate(new ModelWithRequiredArray { Tags = ["a", "b"], Name = "Alice" });

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Items must not be null.", MemberNames = new[] { "Items" } },
            new { ErrorMessage = "Name must not be null.", MemberNames = new[] { "Name" } }
        ]);
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

        results.Should().BeEquivalentTo([
            new { ErrorMessage = "A.B.Value must not be null.", MemberNames = new[] { "A.B.Value" } }
        ]);
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

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
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

        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
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
        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
    }

    [Fact]
    public void Validate_Should_StillValidateTopLevel_When_MaxDepthIsZero()
    {
        var validator = new RequiredPropertiesValidator<ModelWithNestedRequired>(maxDepth: 0);

        var instance = (ModelWithNestedRequired)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ModelWithNestedRequired));

        var results = validator.Validate(instance);

        // Top-level Child is null and depth 0 still covers level 1 properties of T
        results.Should().BeEquivalentTo([
            new { ErrorMessage = "Child must not be null.", MemberNames = new[] { "Child" } }
        ]);
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
        results.Should().BeEquivalentTo([
            new { ErrorMessage = "A.B must not be null.", MemberNames = new[] { "A.B" } }
        ]);
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
        results.Should().BeEquivalentTo(Array.Empty<ValidationResult>());
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
