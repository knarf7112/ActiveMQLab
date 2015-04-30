using ActiveMqLab.Consumer;
using ActiveMqLab.Producer;

namespace BatchAP.POCO
{
    public class MQPublisherAndSubsciber
    {
        public TopicPublisher topicPublisher { get; set; }

        public SynTopicSubscriber synTopicSubscriber { get; set; }
    }
}
