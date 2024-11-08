using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RFCustomSettlements.Quests
{
    public class CustomSettlementQuest : QuestBase
    {
        Func<bool> _completeCondition;
        public CustomSettlementQuest(string questId, Func<bool> CompleteCondition) : base(questId, CharacterObject.PlayerCharacter.HeroObject, CampaignTime.Never, 0)
        {
            _completeCondition = CompleteCondition;
        }

        public override TextObject Title => new TextObject("test quest");

        public override bool IsRemainingTimeHidden => false;

        protected override void HourlyTick()
        {
        }

        protected override void InitializeQuestOnGameLoad()
        {
        }

        protected override void SetDialogs()
        {
        }
        private void CompleteQuest()
        {

        }
        public bool EvaluateCompleteConditions()
        {
            return false;
        }
        public void OnEnemyKilledInCustomSettlement()
        {

        }

    }


    //public class AddQuest
    //{
    //    public static void CreateQuest()
    //    {
    //        // @TODO TEMP
    //        string className = "test";
    //        //

    //        // Dynamic assembly setup
    //        AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
    //        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
    //        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

    //        // Define the class
    //        TypeBuilder typeBuilder = moduleBuilder.DefineType(className,
    //            TypeAttributes.Public | TypeAttributes.Class,
    //            typeof(QuestBase));  // Set base class as Square

    //        Type[] constructorParams = { typeof(string), typeof(Hero), typeof(CampaignTime), typeof(int) };
    //        ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
    //            MethodAttributes.Public,
    //            CallingConventions.Standard,
    //            constructorParams);

    //        //base(questId, questGiver, duration, rewardGold)

    //        // Generate IL code for the constructor to initialize fields
    //        ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
    //        ilGenerator.Emit(OpCodes.Ldarg_0);  // this
    //        ilGenerator.Emit(OpCodes.Ldarg_1);  // questId
    //        ilGenerator.Emit(OpCodes.Ldarg_2);  // questGiver
    //        ilGenerator.Emit(OpCodes.Ldarg_3);  // duration
    //        ilGenerator.Emit(OpCodes.Ldarg, 4); // rewardGold (not used here, specified in xlms)
    //        ConstructorInfo baseConstructor = typeof(QuestBase).GetConstructor(
    //                                            BindingFlags.Instance | BindingFlags.NonPublic,
    //                                            null,
    //                                            new[] { typeof(string), typeof(Hero), typeof(CampaignTime), typeof(int) },
    //                                            null);
    //        ilGenerator.Emit(OpCodes.Call, baseConstructor); // Call base constructor
    //        ilGenerator.Emit(OpCodes.Ret);             // Return from constructor


    //        // 1. Title property
    //        MethodInfo baseTitleGetter = typeof(QuestBase).GetProperty("Title").GetGetMethod();
    //        MethodBuilder titleGetMethod = typeBuilder.DefineMethod(
    //            baseTitleGetter.Name,
    //            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
    //            baseTitleGetter.ReturnType,
    //            Type.EmptyTypes
    //        );
    //        ILGenerator titleGetIL = titleGetMethod.GetILGenerator();
    //        titleGetIL.Emit(OpCodes.Ldstr, "test quest");
    //        titleGetIL.Emit(OpCodes.Ldnull);
    //        titleGetIL.Emit(OpCodes.Newobj, typeof(TextObject).GetConstructor(new[] { typeof(string), typeof(Dictionary<string, object>) }));
    //        titleGetIL.Emit(OpCodes.Ret);
    //        typeBuilder.DefineMethodOverride(titleGetMethod, baseTitleGetter);

    //        // 2. Define the IsRemainingTimeHidden property
    //        MethodInfo remainingTimeGetter = typeof(QuestBase).GetProperty("IsRemainingTimeHidden").GetGetMethod();
    //        MethodBuilder isRemainingTimeHiddenGetMethod = typeBuilder.DefineMethod(
    //            remainingTimeGetter.Name,
    //            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
    //            remainingTimeGetter.ReturnType,
    //            Type.EmptyTypes
    //        );
    //        ILGenerator isRemainingTimeHiddenGetIL = isRemainingTimeHiddenGetMethod.GetILGenerator();
    //        isRemainingTimeHiddenGetIL.Emit(OpCodes.Ldc_I4_0); // Load 'false'
    //        isRemainingTimeHiddenGetIL.Emit(OpCodes.Ret);
    //        typeBuilder.DefineMethodOverride(isRemainingTimeHiddenGetMethod, remainingTimeGetter);

    //        // 3. Define the HourlyTick method
    //        MethodBuilder hourlyTickMethod = typeBuilder.DefineMethod(
    //            "HourlyTick",
    //            MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
    //            typeof(void),
    //            Type.EmptyTypes
    //        );
    //        ILGenerator hourlyTickIL = hourlyTickMethod.GetILGenerator();
    //        hourlyTickIL.Emit(OpCodes.Ret);

    //        // 4. Define the InitializeQuestOnGameLoad method
    //        MethodBuilder initializeQuestOnGameLoadMethod = typeBuilder.DefineMethod(
    //            "InitializeQuestOnGameLoad",
    //            MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
    //            typeof(void),
    //            Type.EmptyTypes
    //        );

    //        // 5. Define the SetDialogs method
    //        MethodBuilder setDialogsMethod = typeBuilder.DefineMethod(
    //            "SetDialogs",
    //            MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
    //            typeof(void),
    //            Type.EmptyTypes
    //        );


    //        ILGenerator initializeQuestOnGameLoadIL = initializeQuestOnGameLoadMethod.GetILGenerator();
    //        initializeQuestOnGameLoadIL.Emit(OpCodes.Ret);

    //        ILGenerator setDialogsIL = setDialogsMethod.GetILGenerator();
    //        setDialogsIL.Emit(OpCodes.Ret);

    //        // Implement the methods in the dynamic type
    //        typeBuilder.DefineMethodOverride(titleGetMethod, typeof(QuestBase).GetProperty("Title").GetGetMethod());
    //        typeBuilder.DefineMethodOverride(isRemainingTimeHiddenGetMethod, typeof(QuestBase).GetProperty("IsRemainingTimeHidden").GetGetMethod());
    //        typeBuilder.DefineMethodOverride(hourlyTickMethod, typeof(QuestBase).GetMethod("HourlyTick", BindingFlags.NonPublic | BindingFlags.Instance));
    //        typeBuilder.DefineMethodOverride(initializeQuestOnGameLoadMethod, typeof(QuestBase).GetMethod("InitializeQuestOnGameLoad", BindingFlags.NonPublic | BindingFlags.Instance));
    //        typeBuilder.DefineMethodOverride(setDialogsMethod, typeof(QuestBase).GetMethod("SetDialogs", BindingFlags.NonPublic | BindingFlags.Instance));



    //        //

    //        //string methodCode = @"public override TextObject Title => new TextObject(""test quest"");";

    //        // Compile the method using Roslyn
    //        //MethodInfo compiledMethod = CompileMethodCode(methodCode, typeBuilder);

    //        // Attach the compiled method to the dynamic type
    //        //typeBuilder.DefineMethodOverride(compiledMethod, typeof(QuestBase).GetMethod("get_Title"));



    //        // Create and return the type
    //        Type dynamicType = typeBuilder.CreateType();

    //        QuestBase instance = (QuestBase)Activator.CreateInstance(dynamicType, "testQuest", Hero.MainHero, CampaignTime.DaysFromNow(50f), 0);
    //        instance.StartQuest();

    //        //var methodsToOverride = typeof(QuestBase).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(m => m.IsAbstract || (m.IsVirtual && !m.IsFinal)).ToList();
    //        // Add an integer field "b"
    //        //FieldBuilder fieldBuilder = typeBuilder.DefineField(additionalFieldName, typeof(JournalLog), FieldAttributes.Public);

    //        //possible use base.addlog

    //        //// Override squareField method
    //        //MethodBuilder methodBuilder = typeBuilder.DefineMethod("squareField",
    //        //    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
    //        //    typeof(int), Type.EmptyTypes);

    //        //ILGenerator ilGenerator = methodBuilder.GetILGenerator();
    //        //ilGenerator.Emit(OpCodes.Ldarg_0); // Load "this"
    //        //ilGenerator.Emit(OpCodes.Ldfld, typeof(Square).GetField("a", BindingFlags.NonPublic | BindingFlags.Instance)); // Load field "a"
    //        //ilGenerator.Emit(OpCodes.Ldarg_0); // Load "this"
    //        //ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder); // Load field "b"
    //        //ilGenerator.Emit(OpCodes.Mul); // Multiply "a" and "b"
    //        //ilGenerator.Emit(OpCodes.Ret);

    //        //typeBuilder.DefineMethodOverride(methodBuilder, typeof(Square).GetMethod("squareField"));

    //    }

    //    private static MethodInfo CompileMethodCode(string code, TypeBuilder typeBuilder)
    //    {
    //        // Set up the C# compilation with Roslyn
    //        var syntaxTree = CSharpSyntaxTree.ParseText(code);
    //        var references = AppDomain.CurrentDomain.GetAssemblies()
    //            .Where(a => !a.IsDynamic)
    //            .Select(a => MetadataReference.CreateFromFile(a.Location))
    //            .ToList();

    //        var compilation = CSharpCompilation.Create("DynamicMethodAssembly")
    //            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
    //            .AddReferences(references)
    //            .AddSyntaxTrees(syntaxTree);

    //        using (var ms = new MemoryStream())
    //        {
    //            var result = compilation.Emit(ms);

    //            if (!result.Success)
    //            {
    //                var errors = string.Join(Environment.NewLine, result.Diagnostics
    //                    .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
    //                    .Select(diagnostic => diagnostic.ToString()));

    //                throw new InvalidOperationException($"Compilation failed:\n{errors}");
    //            }

    //            // Load the compiled assembly containing the method
    //            ms.Seek(0, SeekOrigin.Begin);
    //            var assembly = Assembly.Load(ms.ToArray());

    //            // Get the compiled method info
    //            var dynamicType = assembly.GetType("Rectangle");
    //            return dynamicType.GetMethod("squareField");
    //        }
    //    }
    //}
}
