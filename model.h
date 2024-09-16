#include "flatbuffers/flatbuffers.h"
#include "monster_generated.h"
#include <string>
#include <vector>

namespace fb { namespace game { namespace protocol { namespace model { 

class Response
{
public:
    std::vector<Monster> monsters;

public:
    Response()
    { }

    Response(const std::vector<Monster>& monsters)
        : monsters(monsters)
    { }

    Response(const fb::game::protocol::Response& raw)
    {
        for (int i = 0; i < raw.monsters()->size(); i++)
            this->monsters.push_back(raw.monsters()->Get(i));
    }

private:
    std::vector<flatbuffers::Offset<fb::game::protocol::Monster>> CreateMonsters(flatbuffers::FlatBufferBuilder& builder)
    {
        auto result = std::vector<flatbuffers::Offset<fb::game::protocol::Monster>>;
        for(auto& x : this->monsters)
        {
            result.push_back(x.Build(builder));
        }

        return result;
    }

public:
    flatbuffers::Offset<fb::game::protocol::Response> Build(flatbuffers::FlatBufferBuilder& builder)
    {
        return fb::game::protocol::CreateResponse(builder,
            this->CreateMonsters(builder));
    }
};

} } } }