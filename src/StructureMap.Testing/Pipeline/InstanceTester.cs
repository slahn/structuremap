using System;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using StructureMap.Building;
using StructureMap.Building.Interception;
using StructureMap.Graph;
using StructureMap.Pipeline;
using StructureMap.Testing.Widget;
using StructureMap.Testing.Widget3;
using StructureMap.TypeRules;

namespace StructureMap.Testing.Pipeline
{
    [TestFixture]
    public class InstanceTester
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
        }

        #endregion

        [Test]
        public void Build_the_InstanceToken()
        {
            var instance = new InstanceUnderTest();
            instance.Name = "name of instance";

            var token = instance.CreateToken();

            Assert.AreEqual(instance.Name, token.Name);
            Assert.AreEqual("InstanceUnderTest", token.Description);
        }

        [Test]
        public void has_explicit_name()
        {
            var instance = new InstanceUnderTest();
            instance.HasExplicitName().ShouldBeFalse();

            instance.Name = "name of instance";
            instance.HasExplicitName().ShouldBeTrue();
        }

        [Test]
        public void default_scope_is_PerRequest()
        {
            var i1 = new ConfiguredInstance(GetType()).Named("foo");
            i1.Lifecycle.ShouldBeOfType<TransientLifecycle>();
        }

        [Test]
        public void can_set_scope_directly_on_the_instance()
        {
            var i1 = new ConfiguredInstance(GetType()).Named("foo");
            i1.SetScopeTo(Lifecycles.ThreadLocal);

            i1.Lifecycle.ShouldBeOfType<ThreadLocalStorageLifecycle>();
        }

        [Test]
        public void does_override_the_scope_of_the_parent()
        {
            var family = new PluginFamily(GetType());
            family.SetScopeTo(Lifecycles.Singleton);

            var i1 = new ConfiguredInstance(GetType()).Named("foo");
            i1.SetScopeTo(Lifecycles.ThreadLocal);

            family.AddInstance(i1);

            i1.Lifecycle.ShouldBeOfType<ThreadLocalStorageLifecycle>();
        }

        [Test]
        public void uses_parent_lifecycle_if_none_is_set_on_instance()
        {
            var family = new PluginFamily(GetType());
            family.SetScopeTo(Lifecycles.Singleton);

            var i1 = new ConfiguredInstance(GetType()).Named("foo");

            family.AddInstance(i1);

            i1.Lifecycle.ShouldBeOfType<SingletonLifecycle>();
        }

        [Test]
        public void still_chooses_PerRequest_if_nothing_is_selected_on_either_family_or_instance()
        {
            var family = new PluginFamily(GetType());

            var i1 = new ConfiguredInstance(GetType()).Named("foo");

            family.AddInstance(i1);

            i1.Lifecycle.ShouldBeOfType<TransientLifecycle>();
        }


        [Test]
        public void instance_key_is_predictable()
        {
            var i1 = new ConfiguredInstance(GetType()).Named("foo");
            var i2 = new ConfiguredInstance(GetType()).Named("bar");

            i1.InstanceKey(GetType()).ShouldEqual(i1.InstanceKey(GetType()));
            i2.InstanceKey(GetType()).ShouldEqual(i2.InstanceKey(GetType()));
            i1.InstanceKey(GetType()).ShouldNotEqual(i2.InstanceKey(GetType()));
            i1.InstanceKey(typeof (InstanceUnderTest)).ShouldNotEqual(i1.InstanceKey(GetType()));
        }

        [Test]
        public void add_interceptor_when_the_accept_type_is_possible_on_the_return_type()
        {
            var instance = new InstanceUnderTest
            {
                Type = typeof (StubbedGateway)
            };

            var interceptor = new ActivatorInterceptor<IGateway>(g => g.DoSomething());

            instance.ReturnedType.CanBeCastTo(interceptor.Accepts)
                .ShouldBeTrue();

            instance.AddInterceptor(interceptor);

            instance.Interceptors.Single()
                .ShouldBeTheSameAs(interceptor);
        }

        [Test]
        public void add_interceptor_that_cannot_accept_the_returned_type()
        {
            var instance = new InstanceUnderTest
            {
                Type = typeof(ColorRule)
            };

            var interceptor = new ActivatorInterceptor<IGateway>(g => g.DoSomething());

            instance.ReturnedType.CanBeCastTo(interceptor.Accepts)
                .ShouldBeFalse();

            Exception<ArgumentOutOfRangeException>.ShouldBeThrownBy(() => {
                instance.AddInterceptor(interceptor);
            });
        }
    }

    public class InstanceUnderTest : Instance
    {
        public object TheInstanceThatWasBuilt = new object();

        public override IDependencySource ToDependencySource(Type pluginType)
        {
            return new Constant(pluginType, TheInstanceThatWasBuilt);
        }

        public Type Type { get; set; }

        public override Type ReturnedType
        {
            get
            {
                return Type;
            }
        }

        public override string Description
        {
            get { return "InstanceUnderTest"; }
        }
    }
}