# RF War System + Strategic Intrigue

## Estado

Integração concluída em 3 de agosto de 2026.

O sistema aproveita as ideias estratégicas úteis observadas no Bellum Civile, mas usa a arquitetura própria do Realms Forgotten. Nenhum código do Bellum Civile foi copiado e nenhum sistema paralelo de diplomacia foi criado.

## O que foi implementado

- Registro persistente de cada guerra, inclusive em saves antigos.
- Identificação do reino iniciador e do motivo da guerra.
- Motivos especiais: reconquista, guerra defensiva, guerra santa, punição, expansão, contenção, Grand Design, Strategic Intrigue, contrato mercenário, guerra de alinhamento, defesa coletiva e rivalidade duradoura.
- Pontuação própria de guerra baseada em batalhas, grandes batalhas, cidades, castelos, retomadas, aldeias saqueadas e nobres ou governantes capturados.
- Registro dos objetivos territoriais escolhidos pelo diretor de campanha.
- Exaustão de guerra calculada por duração, derrotas, perda de feudos, saques, cativeiros, múltiplas frentes, tesouro e inatividade.
- Vontade de continuar a guerra baseada no motivo, perfil cultural, objetivo atual, vantagem, rivalidade e exaustão.
- Pressão de paz para guerras caras, perdidas ou paralisadas.
- Pressão de finalização para evitar que um reino abandone uma ofensiva decisiva quando o inimigo está perto do colapso.
- Memória histórica de derrotas e humilhações, usada como motivo de revanche em guerras futuras.
- Integração desses valores no modelo de diplomacia e no planejador de decisões do RF.
- Integração com o Strategic Intrigue: a exaustão política agora lê a situação estratégica real.
- Relatório do War Table com score, motivo, exaustão, vontade de lutar e pressão de paz.
- Diagnóstico detalhado do ledger para inspeção futura.

## Compatibilidade com saves

O mesmo identificador de save anterior foi mantido. O formato interno passou de V1 para V2, mas o carregador aceita ambos.

- Save criado antes do ledger: guerras ativas são reconstruídas automaticamente.
- Save com ledger V1: os eventos antigos são preservados; iniciador e motivo recebem valores seguros de recuperação.
- Save novo: todos os novos campos são gravados normalmente.

## Como os sistemas trabalham juntos

1. Uma guerra começa e o ledger registra iniciador, motivo e situação territorial inicial.
2. Batalhas, conquistas, saques, cativeiros e objetivos atualizam o histórico.
3. O avaliador transforma o histórico em exaustão, vontade, impasse e pressão de finalização.
4. O modelo diplomático usa esses valores ao avaliar guerra e paz.
5. O planejador usa os mesmos valores para escolher qual proposta política realmente deve avançar.
6. O Strategic Intrigue usa a exaustão real para afetar pressão da corte e legitimidade.

## Teste recomendado no jogo

1. Carregar um save antigo com uma guerra ativa e avançar dois dias.
2. Confirmar que o jogo não cria paz imediata em uma guerra recém-iniciada.
3. Vencer e perder batalhas grandes e observar se o comportamento diplomático muda gradualmente.
4. Capturar um castelo ou cidade e verificar se o reino vencedor tenta consolidar ou concluir a ofensiva.
5. Manter uma guerra equilibrada sem progresso por algumas semanas e verificar se a pressão por paz aumenta.
6. Abrir o War Table do Strategic Intrigue e conferir a seção `Strategic war state`.
7. Encerrar uma guerra perdida, esperar algum tempo e observar se um reino com perfil vingativo volta a considerar o antigo inimigo.

## Limites intencionais

- Nenhuma imagem, prefab, atlas ou interface Gauntlet foi alterada.
- O sistema feudal, sucessório e de títulos do Bellum Civile não foi importado porque não pertence ao sistema de guerra/diplomacia solicitado.
- Os valores são limitados e graduais para evitar declarações e tratados em sequência rápida.
